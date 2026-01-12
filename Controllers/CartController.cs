using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KD_Restaurant.Models;
using KD_Restaurant.Extensions;
using Microsoft.EntityFrameworkCore;
using KD_Restaurant.ViewModels;
using KD_Restaurant.Utilities;
using KD_Restaurant.Services;
using KD_Restaurant.Services.Models;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Net.Mail;

namespace KD_Restaurant.Controllers
{
    public class CartController : Controller
    {
        private readonly KDContext _context;
        private readonly IMomoPaymentService _momoPaymentService;
        private readonly ILogger<CartController> _logger;

        public CartController(KDContext context, IMomoPaymentService momoPaymentService, ILogger<CartController> logger)
        {
            _context = context;
            _momoPaymentService = momoPaymentService;
            _logger = logger;
        }

        // Hiển thị giỏ hàng
        public async Task<IActionResult> Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            var branches = await _context.tblBranch
                .Where(b => b.IsActive)
                .OrderBy(b => b.BranchName)
                .AsNoTracking()
                .ToListAsync();

            string? defaultName = null;
            string? defaultPhone = null;
            string? defaultEmail = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdValue, out var userId))
                {
                    var currentUser = await _context.tblUser
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.IdUser == userId);

                    if (currentUser != null)
                    {
                        var linkedCustomer = await _context.tblCustomer
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.IdUser == currentUser.IdUser);

                        var parts = new[] { currentUser.LastName, currentUser.FirstName }
                            .Where(part => !string.IsNullOrWhiteSpace(part))
                            .ToArray();

                        var fallbackName = parts.Length > 0 ? string.Join(" ", parts) : currentUser.UserName;
                        var fallbackEmail = NormalizeEmail(currentUser.UserName);

                        if (linkedCustomer != null)
                        {
                            defaultName = !string.IsNullOrWhiteSpace(linkedCustomer.FullName)
                                ? linkedCustomer.FullName
                                : fallbackName;

                            defaultPhone = !string.IsNullOrWhiteSpace(linkedCustomer.PhoneNumber)
                                ? linkedCustomer.PhoneNumber
                                : currentUser.PhoneNumber;

                            var customerEmail = NormalizeEmail(linkedCustomer.Email);
                            defaultEmail = !string.IsNullOrWhiteSpace(customerEmail)
                                ? customerEmail
                                : fallbackEmail;
                        }
                        else
                        {
                            defaultName = fallbackName;
                            defaultPhone = currentUser.PhoneNumber;
                            if (!string.IsNullOrWhiteSpace(fallbackEmail))
                            {
                                defaultEmail = fallbackEmail;
                            }
                        }
                    }
                }
            }

            var model = new CartPageViewModel
            {
                Items = cart,
                Branches = branches,
                TimeSlots = BookingTimeSlotProvider.GetDefaultSlots(),
                DefaultFullName = defaultName,
                DefaultPhoneNumber = defaultPhone,
                DefaultEmail = defaultEmail,
                IsAuthenticated = User.Identity?.IsAuthenticated == true
            };

            return View(model);
        }

        // Thêm món vào giỏ hàng
        [HttpPost]
        public IActionResult AddToCart(int id, int quantity)
        {
            var menuItem = _context.tblMenuItem.FirstOrDefault(x => x.IdMenuItem == id);
            if (menuItem == null) return Json(new { success = false, message = "Không tìm thấy món ăn" });

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var item = cart.FirstOrDefault(x => x.IdMenuItem == id);
            if (item != null)
            {
                item.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    IdMenuItem = menuItem.IdMenuItem,
                    Title = menuItem.Title,
                    Image = menuItem.Image,
                   Price = menuItem.PriceSale.HasValue ? menuItem.PriceSale.Value : menuItem.Price,

                    Quantity = quantity
                });
            }
            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return Json(new { success = true, message = "Đã thêm vào giỏ hàng" });
        }

        // Xóa món khỏi giỏ hàng
        [HttpPost]
        public IActionResult RemoveFromCart(int id)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var item = cart.FirstOrDefault(x => x.IdMenuItem == id);
            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
            return Json(new { success = true });
        }

        // Cập nhật số lượng
        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var item = cart.FirstOrDefault(x => x.IdMenuItem == id);
            if (item != null && quantity > 0)
            {
                item.Quantity = quantity;
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
            return Json(new { success = true });
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            int IdBranch,
            DateTime BookingDate,
            string TimeSlot,
            int NumberGuests,
            string Note,
            string FullName,
            string PhoneNumber,
            string Email)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (!cart.Any())
            {
                TempData["PaymentError"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction(nameof(Index));
            }

            var normalizedEmail = NormalizeEmail(Email);
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                TempData["BookingError"] = "Vui lòng nhập email hợp lệ để nhận thông báo đặt bàn.";
                return RedirectToAction(nameof(Index));
            }

            var trimmedPhone = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim();
            var trimmedName = string.IsNullOrWhiteSpace(FullName) ? null : FullName.Trim();

            int? currentUserId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdValue, out var parsedUserId))
                {
                    currentUserId = parsedUserId;
                }
            }

            tblCustomer? customer = null;
            if (currentUserId.HasValue)
            {
                customer = await _context.tblCustomer.FirstOrDefaultAsync(c => c.IdUser == currentUserId.Value);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(trimmedPhone))
            {
                customer = await _context.tblCustomer.FirstOrDefaultAsync(x => x.PhoneNumber == trimmedPhone);
            }

            if (customer == null)
            {
                customer = new tblCustomer
                {
                    IdUser = currentUserId,
                    FullName = trimmedName,
                    PhoneNumber = trimmedPhone,
                    Email = normalizedEmail,
                    IsActive = true
                };
                _context.tblCustomer.Add(customer);
                await _context.SaveChangesAsync();
            }
            else
            {
                if (currentUserId.HasValue && customer.IdUser != currentUserId)
                {
                    customer.IdUser = currentUserId.Value;
                }

                if (!string.IsNullOrWhiteSpace(trimmedName))
                {
                    customer.FullName = trimmedName;
                }

                if (!string.IsNullOrWhiteSpace(trimmedPhone))
                {
                    customer.PhoneNumber = trimmedPhone;
                }

                if (string.IsNullOrWhiteSpace(customer.Email) ||
                    !string.Equals(customer.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
                {
                    customer.Email = normalizedEmail;
                }

                if (!customer.IsActive)
                {
                    customer.IsActive = true;
                }
            }

            var persistedCustomer = customer!;

            var trimmedSlot = TimeSlot?.Trim() ?? string.Empty;
            var booking = new tblBooking
            {
                IdBranch = IdBranch,
                BookingDate = BookingDate.Date,
                TimeSlot = trimmedSlot,
                NumberGuests = Math.Max(1, NumberGuests),
                Note = string.IsNullOrWhiteSpace(Note) ? "Khách chưa để lại ghi chú" : Note.Trim(),
                isActive = true,
                IdCustomer = persistedCustomer.IdCustomer,
                IdStatus = 1,
                Email = normalizedEmail
            };
            _context.tblBooking.Add(booking);
            await _context.SaveChangesAsync();

            var order = new tblOrder
            {
                IdBooking = booking.IdBooking,
                OrderDate = DateTime.Now,
                TotalAmount = 0,
                Status = 1,
                PaymentMethod = "Chưa xác định",
                Notes = booking.Note
            };
            _context.tblOrder.Add(order);
            await _context.SaveChangesAsync();

            var total = 0;
            foreach (var item in cart)
            {
                var lineAmount = item.Price * item.Quantity;
                var detail = new tblOrder_detail
                {
                    IdOrder = order.IdOrder,
                    IdMenuItem = item.IdMenuItem,
                    Quantity = item.Quantity,
                    PriceSale = item.Price,
                    Amount = lineAmount
                };

                total += lineAmount;
                _context.tblOrder_detail.Add(detail);
            }

            order.TotalAmount = total;
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("Cart");
            TempData.Remove("BookingError");

            return RedirectToAction(nameof(Checkout), new { id = order.IdOrder });
        }

        public async Task<IActionResult> Checkout(int id)
        {
            var order = await _context.tblOrder
                .Include(o => o.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(o => o.Booking)
                    .ThenInclude(b => b.Branch)
                .FirstOrDefaultAsync(o => o.IdOrder == id);

            if (order == null)
            {
                TempData["PaymentError"] = "Không tìm thấy đơn đặt bàn để thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            var orderDetails = await _context.tblOrder_detail
                .Where(d => d.IdOrder == id)
                .Include(d => d.MenuItem)
                .ToListAsync();

            ViewBag.Order = order;
            ViewBag.OrderDetails = orderDetails;
            ViewBag.PaymentError = TempData["PaymentError"];

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id, string paymentMethod)
        {
            var order = await _context.tblOrder
                .Include(o => o.Booking)
                .ThenInclude(b => b.Customer)
                .FirstOrDefaultAsync(o => o.IdOrder == id);

            if (order == null)
            {
                TempData["PaymentError"] = "Không tìm thấy đơn đặt bàn.";
                return RedirectToAction(nameof(Index));
            }

            var normalizedMethod = paymentMethod?.Trim() ?? string.Empty;
            var displayMethod = ResolvePaymentMethodName(normalizedMethod);

            if (IsMomoPayment(normalizedMethod))
            {
                if (!order.TotalAmount.HasValue || order.TotalAmount.Value <= 0)
                {
                    TempData["PaymentError"] = "Đơn đặt bàn chưa có tổng tiền để thanh toán.";
                    return RedirectToAction(nameof(Checkout), new { id });
                }

                var request = new MomoPaymentRequest
                {
                    OrderId = $"{order.IdOrder}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                    OrderInfo = $"Thanh toán MoMo cho đơn #{order.IdOrder}",
                    Amount = order.TotalAmount.Value,
                    ReturnUrl = BuildAbsoluteUrl(nameof(MomoReturn)) ?? string.Empty,
                    NotifyUrl = BuildAbsoluteUrl(nameof(MomoNotify)) ?? string.Empty,
                    ExtraData = EncodeExtraData(order.IdOrder, normalizedMethod)
                };

                var result = await _momoPaymentService.CreatePaymentAsync(request);
                if (!result.Success || string.IsNullOrWhiteSpace(result.PayUrl))
                {
                    TempData["PaymentError"] = result.Message ?? "Không tạo được liên kết thanh toán MoMo.";
                    return RedirectToAction(nameof(Checkout), new { id });
                }

                order.PaymentMethod = displayMethod;
                await _context.SaveChangesAsync();

                return Redirect(result.PayUrl);
            }

            order.PaymentMethod = displayMethod;
            order.PaymentTime = null;
            if (order.Booking != null)
            {
                order.Booking.IdStatus = 1;
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("Cart");

            TempData["SuccessMessage"] = "Đặt bàn thành công! Bạn sẽ thanh toán trực tiếp theo phương thức đã chọn.";
            return RedirectToAction(nameof(Success));
        }
        
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> MomoReturn([FromQuery] MomoCallbackModel callback)
        {
            var orderId = TryExtractOrderId(callback);
            if (orderId == null)
            {
                TempData["PaymentError"] = "Không xác định được đơn đặt bàn từ phản hồi MoMo.";
                return RedirectToAction(nameof(Index));
            }

            var order = await _context.tblOrder
                .Include(o => o.Booking)
                    .ThenInclude(b => b.Customer)
                .FirstOrDefaultAsync(o => o.IdOrder == orderId.Value);

            if (order == null)
            {
                TempData["PaymentError"] = "Không tìm thấy đơn đặt bàn để cập nhật.";
                return RedirectToAction(nameof(Index));
            }

            if (callback.ResultCode == 0)
            {
                var methodName = string.IsNullOrWhiteSpace(order.PaymentMethod) ? "Ví MoMo" : order.PaymentMethod;
                await MarkOrderPaidAsync(order, methodName);
                HttpContext.Session.Remove("Cart");
                TempData["SuccessMessage"] = "Thanh toán qua MoMo thành công. Hẹn gặp bạn tại nhà hàng!";
                return RedirectToAction(nameof(Success));
            }

            _logger.LogWarning("MoMo return thất bại cho đơn {OrderId}: {Message}", order.IdOrder, callback.Message);
            TempData["PaymentError"] = $"Thanh toán MoMo thất bại (mã {callback.ResultCode}).";
            return RedirectToAction(nameof(Checkout), new { id = order.IdOrder });
        }

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MomoNotify()
        {
            string body;
            using (var reader = new StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            MomoCallbackModel? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<MomoCallbackModel>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không phân tích được IPN MoMo: {Body}", body);
            }

            if (payload != null)
            {
                var orderId = TryExtractOrderId(payload);
                if (orderId.HasValue)
                {
                    var order = await _context.tblOrder
                        .Include(o => o.Booking)
                            .ThenInclude(b => b.Customer)
                        .FirstOrDefaultAsync(o => o.IdOrder == orderId.Value);

                    if (order != null && payload.ResultCode == 0)
                    {
                        var methodName = string.IsNullOrWhiteSpace(order.PaymentMethod) ? "Ví MoMo" : order.PaymentMethod;
                        await MarkOrderPaidAsync(order, methodName);
                    }
                }
            }

            return Json(new { resultCode = 0, message = "Success" });
        }

        // Trang thông báo thành công
        public IActionResult Success()
        {
            ViewBag.SuccessMessage = TempData["SuccessMessage"] ?? "Đơn đặt bàn của bạn đã được xác nhận.";
            return View();
        }

        private async Task MarkOrderPaidAsync(tblOrder order, string paymentMethod)
        {
            if (order.Booking == null)
            {
                await _context.Entry(order).Reference(o => o.Booking).LoadAsync();
            }

            order.PaymentMethod = paymentMethod;
            order.PaymentTime = DateTime.Now;
            order.Status = 1;

            if (order.Booking != null && (!order.Booking.IdStatus.HasValue || order.Booking.IdStatus == 1))
            {
                order.Booking.IdStatus = 1;
            }

            await _context.SaveChangesAsync();
        }

        private string? BuildAbsoluteUrl(string actionName)
        {
            var host = Request.Host.HasValue ? Request.Host.Value : null;
            return Url.Action(actionName, "Cart", values: null, protocol: Request.Scheme, host: host);
        }

        private static string EncodeExtraData(int orderId, string? paymentCode)
        {
            var payload = string.IsNullOrWhiteSpace(paymentCode)
                ? orderId.ToString()
                : $"{orderId}|{paymentCode}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
        }

        private static int? TryExtractOrderId(MomoCallbackModel callback)
        {
            if (!string.IsNullOrWhiteSpace(callback.ExtraData))
            {
                try
                {
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(callback.ExtraData));
                    var parts = decoded.Split('|');
                    if (parts.Length > 0 && int.TryParse(parts[0], out var parsed))
                    {
                        return parsed;
                    }
                }
                catch
                {
                    // ignore
                }
            }

            if (!string.IsNullOrWhiteSpace(callback.OrderId))
            {
                var candidate = callback.OrderId.Split('-').FirstOrDefault();
                if (int.TryParse(candidate, out var parsed))
                {
                    return parsed;
                }
            }

            return null;
        }

        private static string? NormalizeEmail(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            try
            {
                var address = new MailAddress(value.Trim());
                return address.Address;
            }
            catch
            {
                return null;
            }
        }

        public class MomoCallbackModel
        {
            public string? OrderId { get; set; }
            public string? ExtraData { get; set; }
            public int ResultCode { get; set; }
            public string? Message { get; set; }
        }
        
        private static bool IsMomoPayment(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            return code.Equals("MOMO", StringComparison.OrdinalIgnoreCase)
                || code.StartsWith("MOMO_", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolvePaymentMethodName(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return "Thanh toán tại nhà hàng";
            }

            return code.ToUpperInvariant() switch
            {
                "MOMO" or "MOMO_WALLET" => "Ví MoMo",
                "MOMO_ATM" => "Thẻ ATM nội địa (qua MoMo)",
                "MOMO_CARD" => "Visa/Master/JCB (qua MoMo)",
                "BANK_TRANSFER" => "Chuyển khoản ngân hàng",
                _ => "Thanh toán tại nhà hàng"
            };
        }
    }
}