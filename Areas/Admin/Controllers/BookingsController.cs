using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KD_Restaurant.Models;
using KD_Restaurant.Security;
using KD_Restaurant.Utilities;
using KD_Restaurant.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using KD_Restaurant.Services;

namespace KD_Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.AdminManagerStaff)]
    [PermissionAuthorize(PermissionKeys.BookingManagement)]
    public class BookingsController : Controller
    {
        private readonly KDContext _context;
        private readonly ILogger<BookingsController> _logger;
        private readonly IBookingNotificationService _bookingNotificationService;
        private int? _reservedTableStatusId;
        private int? _availableTableStatusId;
        private int? _servingTableStatusId;
        private int? _servingBookingStatusId;

        public BookingsController(KDContext context, ILogger<BookingsController> logger, IBookingNotificationService bookingNotificationService)
        {
            _context = context;
            _logger = logger;
            _bookingNotificationService = bookingNotificationService;
        }

        public async Task<IActionResult> Index(DateTime? date = null, int? branchId = null, int? statusId = null)
        {
            var targetDate = (date ?? DateTime.Today).Date;

            var bookingQuery = _context.tblBooking
                .Include(b => b.Customer)
                .Include(b => b.Branch)
                .Include(b => b.Table)
                    .ThenInclude(t => t!.Type)
                .Include(b => b.Status)
                .Where(b => b.isActive && b.BookingDate.Date == targetDate);

            var servingStatusId = await GetServingBookingStatusIdAsync();
            if (!statusId.HasValue && servingStatusId.HasValue)
            {
                bookingQuery = bookingQuery.Where(b => b.IdStatus != servingStatusId.Value);
            }

            if (branchId.HasValue)
            {
                bookingQuery = bookingQuery.Where(b => b.IdBranch == branchId);
            }

            if (statusId.HasValue)
            {
                bookingQuery = bookingQuery.Where(b => b.IdStatus == statusId);
            }

            var bookings = await bookingQuery
                .OrderBy(b => b.TimeSlot)
                .ThenBy(b => b.Customer.FullName)
                .ToListAsync();

            var bookingIds = bookings.Select(b => b.IdBooking).ToList();
            var orderItemsLookup = await BuildOrderLookupAsync(bookingIds);

            var bookingCards = bookings.Select(b =>
            {
                var statusId = b.IdStatus ?? 1;
                var statusName = b.Status?.StatusName ?? BookingStatusHelper.GetStatusName(statusId);
                var badgeClass = BookingStatusHelper.GetBadgeClass(statusName);
                var statusKey = BookingStatusHelper.GetStatusKey(statusId);

                return new BookingCardViewModel
                {
                    Id = b.IdBooking,
                    CustomerName = b.Customer?.FullName ?? "Khách lẻ",
                    CustomerPhone = b.Customer?.PhoneNumber ?? string.Empty,
                    BranchId = b.IdBranch,
                    BranchName = b.Branch?.BranchName,
                    TableName = b.Table?.TableName,
                    TableId = b.IdTable,
                    BookingDate = b.BookingDate,
                    TimeSlot = b.TimeSlot ?? string.Empty,
                    Guests = b.NumberGuests,
                    Note = b.Note,
                    StatusId = statusId,
                    StatusName = statusName,
                    StatusBadgeClass = badgeClass,
                    StatusKey = statusKey,
                    HasOrder = orderItemsLookup.ContainsKey(b.IdBooking),
                    OrderItems = orderItemsLookup.TryGetValue(b.IdBooking, out var items) ? items : new List<BookingOrderItemViewModel>()
                };
            }).ToList();

            var statusEntities = await _context.tblBooking_status
                .Where(s => s.isActive)
                .OrderBy(s => s.StatusName)
                .Select(s => new { s.IdStatus, s.StatusName })
                .ToListAsync();

            var statuses = statusEntities
                .Select(s => new BookingStatusOption
                {
                    Id = s.IdStatus,
                    Name = s.StatusName ?? "Không xác định",
                    BadgeClass = BookingStatusHelper.GetBadgeClass(s.StatusName)
                })
                .ToList();

            var branches = await _context.tblBranch
                .Where(b => b.IsActive)
                .OrderBy(b => b.BranchName)
                .Select(b => new SelectOption
                {
                    Id = b.IdBranch,
                    Name = string.IsNullOrWhiteSpace(b.BranchName) ? $"Chi nhánh #{b.IdBranch}" : b.BranchName!
                })
                .ToListAsync();

            var customers = await _context.tblCustomer
                .Where(c => c.IsActive)
                .OrderBy(c => c.FullName)
                .Select(c => new SelectOption
                {
                    Id = c.IdCustomer,
                    Name = string.IsNullOrWhiteSpace(c.FullName) ? $"Khách #{c.IdCustomer}" : c.FullName!
                })
                .ToListAsync();

            var menuItems = await _context.tblMenuItem
                .Where(m => m.IsActive)
                .OrderBy(m => m.Title)
                .Select(m => new SelectOption
                {
                    Id = m.IdMenuItem,
                    Name = m.Title ?? $"Món #{m.IdMenuItem}"
                })
                .ToListAsync();

            var counters = bookingCards
                .GroupBy(b => b.StatusId ?? -1)
                .ToDictionary(g => g.Key, g => g.Count());

            var viewModel = new BookingManagementViewModel
            {
                SelectedDate = targetDate,
                SelectedBranchId = branchId,
                SelectedStatusId = statusId,
                Bookings = bookingCards,
                Branches = branches,
                Statuses = statuses,
                Customers = customers,
                MenuItems = menuItems,
                StatusCounters = counters
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                return Json(new { success = false, message = error });
            }

            var bookingDate = request.BookingDate.Date;
            tblCustomer? customer = null;

            if (request.CustomerId.HasValue)
            {
                customer = await _context.tblCustomer.FirstOrDefaultAsync(c => c.IdCustomer == request.CustomerId.Value);
                if (customer == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khách hàng đã chọn." });
                }
            }

            if (customer == null && !string.IsNullOrWhiteSpace(request.CustomerName))
            {
                customer = new tblCustomer
                {
                    FullName = request.CustomerName,
                    PhoneNumber = request.CustomerPhone,
                    IsActive = true
                };
                _context.tblCustomer.Add(customer);
                await _context.SaveChangesAsync();
            }

            var normalizedTimeSlot = (request.TimeSlot ?? string.Empty).Trim();

            var booking = new tblBooking
            {
                IdCustomer = customer?.IdCustomer,
                IdBranch = request.BranchId,
                BookingDate = bookingDate,
                TimeSlot = normalizedTimeSlot,
                NumberGuests = request.NumberGuests,
                Note = request.Note,
                isActive = true,
                IdStatus = await GetDefaultBookingStatusIdAsync()
            };

            if (request.TableId.HasValue)
            {
                var table = await _context.tblTables
                    .Include(t => t.Area)
                    .FirstOrDefaultAsync(t => t.IdTable == request.TableId.Value && t.isActive);

                if (table == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bàn phù hợp." });
                }

                if (!await IsTableAvailableAsync(table.IdTable, bookingDate, normalizedTimeSlot, null))
                {
                    return Json(new { success = false, message = "Bàn đã được đặt trong khung giờ này." });
                }

                booking.IdTable = table.IdTable;

                if (!booking.IdBranch.HasValue && table.Area?.IdBranch != null)
                {
                    booking.IdBranch = table.Area.IdBranch;
                }

                var reservedStatusId = await GetReservedTableStatusIdAsync();
                if (reservedStatusId.HasValue)
                {
                    table.IdStatus = reservedStatusId.Value;
                }
            }

            _context.tblBooking.Add(booking);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tạo đặt bàn mới");
                return Json(new { success = false, message = "Không thể lưu đặt bàn. Vui lòng thử lại." });
            }

            return Json(new { success = true, redirectUrl = Url.Action(nameof(Index), new { date = bookingDate.ToString("yyyy-MM-dd") }) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTable(BookingAssignRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Thông tin gắn bàn không hợp lệ." });
            }

            var booking = await _context.tblBooking
                .Include(b => b.Customer)
                .Include(b => b.Branch)
                .FirstOrDefaultAsync(b => b.IdBooking == request.BookingId);
            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đặt bàn." });
            }

            var table = await _context.tblTables
                .Include(t => t.Area)
                    .ThenInclude(a => a!.Branch)
                .FirstOrDefaultAsync(t => t.IdTable == request.TableId && t.isActive);

            if (table == null)
            {
                return Json(new { success = false, message = "Bàn không tồn tại hoặc đã ngưng sử dụng." });
            }

            var bookingSlot = (booking.TimeSlot ?? string.Empty).Trim();
            if (!await IsTableAvailableAsync(table.IdTable, booking.BookingDate.Date, bookingSlot, booking.IdBooking))
            {
                return Json(new { success = false, message = "Bàn đã được đặt trong khung giờ này." });
            }

            var availableStatusId = await GetAvailableTableStatusIdAsync();
            if (booking.IdTable.HasValue && booking.IdTable != table.IdTable && availableStatusId.HasValue)
            {
                var previousTable = await _context.tblTables.FirstOrDefaultAsync(t => t.IdTable == booking.IdTable.Value);
                if (previousTable != null)
                {
                    previousTable.IdStatus = availableStatusId.Value;
                }
            }

            booking.IdTable = table.IdTable;

            if (!booking.IdBranch.HasValue && table.Area?.IdBranch != null)
            {
                booking.IdBranch = table.Area.IdBranch;
            }

            var reservedStatusId = await GetReservedTableStatusIdAsync();
            if (reservedStatusId.HasValue)
            {
                table.IdStatus = reservedStatusId.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể gắn bàn cho đặt bàn {BookingId}", request.BookingId);
                return Json(new { success = false, message = "Không thể lưu thay đổi. Vui lòng thử lại." });
            }

            await _bookingNotificationService.SendTableAssignmentEmailAsync(booking, table);

            return Json(new
            {
                success = true,
                table = new
                {
                    id = table.IdTable,
                    name = table.TableName ?? $"Bàn #{table.IdTable}",
                    area = table.Area?.AreaName
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int bookingId)
        {
            var booking = await _context.tblBooking
                .Include(b => b.Table)
                .FirstOrDefaultAsync(b => b.IdBooking == bookingId && b.isActive);

            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đặt bàn." });
            }

            if (!booking.IdTable.HasValue)
            {
                return Json(new { success = false, message = "Đặt bàn này chưa được xếp bàn." });
            }

            var table = booking.Table ?? await _context.tblTables.FirstOrDefaultAsync(t => t.IdTable == booking.IdTable.Value);
            if (table == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin bàn tương ứng." });
            }

            var order = await _context.tblOrder.FirstOrDefaultAsync(o => o.IdBooking == booking.IdBooking);
            if (order == null)
            {
                order = new tblOrder
                {
                    IdBooking = booking.IdBooking,
                    IdCustomer = booking.IdCustomer,
                    IdTable = booking.IdTable,
                    OrderDate = DateTime.Now,
                    TimeIn = DateTime.Now
                };
                _context.tblOrder.Add(order);
            }
            else
            {
                order.IdTable = booking.IdTable;
                order.TimeIn ??= DateTime.Now;
                if (order.OrderDate == default)
                {
                    order.OrderDate = DateTime.Now;
                }
            }

            var servingTableStatusId = await GetServingTableStatusIdAsync();
            if (servingTableStatusId.HasValue)
            {
                table.IdStatus = servingTableStatusId.Value;
            }

            var servingBookingStatusId = await GetServingBookingStatusIdAsync();
            if (servingBookingStatusId.HasValue)
            {
                booking.IdStatus = servingBookingStatusId.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể xác nhận khách nhận bàn cho đặt bàn {BookingId}", bookingId);
                return Json(new { success = false, message = "Không thể cập nhật trạng thái. Vui lòng thử lại." });
            }

            return Json(new
            {
                success = true,
                redirectUrl = Url.Action("Index", "Book", new { area = "Admin", highlightBookingId = booking.IdBooking })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(BookingPaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Thông tin thanh toán không hợp lệ." });
            }

            var bookingId = request.BookingId;
            var order = await _context.tblOrder
                .Include(o => o.Booking)
                    .ThenInclude(b => b.Table)
                .Include(o => o.tblOrder_detail)
                .FirstOrDefaultAsync(o => o.IdBooking == bookingId);

            if (order == null)
            {
                return Json(new { success = false, message = "Chưa có hoá đơn để thanh toán." });
            }

            var booking = order.Booking;

            var totalAmount = order.tblOrder_detail?.Sum(d => d.Amount) ?? 0;
            order.TotalAmount = totalAmount;
            order.TotalCost = totalAmount;
            order.TimeOut ??= DateTime.Now;
            order.PaymentTime = DateTime.Now;
            order.Status = 1;
            order.PaymentMethod = request.PaymentMethod;
            if (request.AmountGiven < totalAmount)
            {
                return Json(new { success = false, message = "Số tiền khách trả chưa đủ để thanh toán." });
            }

            var changeAmount = Math.Max(0, request.AmountGiven - totalAmount);
            var paymentNote = request.Notes;
            if (request.AmountGiven > 0)
            {
                var cashLine = string.Format("Khách trả {0:N0} ₫ · Thối lại {1:N0} ₫", request.AmountGiven, changeAmount);
                paymentNote = string.IsNullOrWhiteSpace(paymentNote) ? cashLine : $"{paymentNote}\n{cashLine}";
            }

            order.Notes = paymentNote;

            booking.isActive = false;
            booking.IdStatus = 4;

            var availableStatusId = await GetAvailableTableStatusIdAsync();
            if (availableStatusId.HasValue && booking.Table != null)
            {
                booking.Table.IdStatus = availableStatusId.Value;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể hoàn tất hoá đơn cho đặt bàn {BookingId}", bookingId);
                return Json(new { success = false, message = "Không thể thanh toán hoá đơn. Vui lòng thử lại." });
            }

            var response = new
            {
                success = true,
                redirectUrl = Url.Action(nameof(Index), new { date = booking.BookingDate.ToString("yyyy-MM-dd") }),
                receiptUrl = request.PrintReceipt
                    ? Url.Action(nameof(Invoice), new { bookingId = booking.IdBooking, autoPrint = true })
                    : null
            };

            return Json(response);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMenuItem(BookingMenuRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Thông tin món không hợp lệ." });
            }

            var booking = await _context.tblBooking.FirstOrDefaultAsync(b => b.IdBooking == request.BookingId);
            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đặt bàn." });
            }

            var menuItem = await _context.tblMenuItem.FirstOrDefaultAsync(m => m.IdMenuItem == request.MenuItemId && m.IsActive);
            if (menuItem == null)
            {
                return Json(new { success = false, message = "Món không tồn tại hoặc đã ngưng phục vụ." });
            }

            var order = await _context.tblOrder.FirstOrDefaultAsync(o => o.IdBooking == booking.IdBooking);
            if (order == null)
            {
                order = new tblOrder
                {
                    IdBooking = booking.IdBooking,
                    IdCustomer = booking.IdCustomer,
                    IdTable = booking.IdTable,
                    OrderDate = DateTime.Now,
                    Status = null
                };
                _context.tblOrder.Add(order);
                await _context.SaveChangesAsync();
            }

            var unitPrice = menuItem.PriceSale ?? menuItem.Price;

            var detail = new tblOrder_detail
            {
                IdOrder = order.IdOrder,
                IdMenuItem = menuItem.IdMenuItem,
                Quantity = request.Quantity,
                PriceSale = unitPrice,
                Amount = unitPrice * request.Quantity
            };

            _context.tblOrder_detail.Add(detail);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể thêm món cho đặt bàn {BookingId}", request.BookingId);
                return Json(new { success = false, message = "Không thể lưu món. Vui lòng thử lại." });
            }

            var orderTotal = await CalculateOrderTotalAsync(booking.IdBooking);

            return Json(new
            {
                success = true,
                item = new
                {
                    id = detail.Id,
                    name = menuItem.Title ?? $"Món #{menuItem.IdMenuItem}",
                    quantity = detail.Quantity ?? 0,
                    unitPrice = unitPrice,
                    lineTotal = detail.Amount
                },
                totals = new
                {
                    orderTotal
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderItem(BookingOrderUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Số lượng món không hợp lệ." });
            }

            var detail = await _context.tblOrder_detail
                .Include(d => d.MenuItem)
                .Include(d => d.Order)
                .FirstOrDefaultAsync(d => d.Id == request.DetailId);

            if (detail == null)
            {
                return Json(new { success = false, message = "Không tìm thấy món cần cập nhật." });
            }

            if (request.Quantity == 0)
            {
                _context.tblOrder_detail.Remove(detail);
            }
            else
            {
                var unitPrice = detail.PriceSale ?? detail.MenuItem?.Price ?? 0;
                detail.Quantity = request.Quantity;
                detail.Amount = unitPrice * request.Quantity;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật món {DetailId}", request.DetailId);
                return Json(new { success = false, message = "Không thể cập nhật món. Vui lòng thử lại." });
            }

            var bookingId = detail.Order.IdBooking ?? 0;
            var orderTotal = await CalculateOrderTotalAsync(bookingId);

            return Json(new
            {
                success = true,
                removed = request.Quantity == 0,
                item = request.Quantity == 0 ? null : new
                {
                    id = detail.Id,
                    quantity = detail.Quantity ?? 0,
                    lineTotal = detail.Amount
                },
                totals = new
                {
                    orderTotal
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(BookingCancelRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Thông tin huỷ không hợp lệ." });
            }

            var booking = await _context.tblBooking
                .Include(b => b.Table)
                .Include(b => b.Customer)
                .Include(b => b.Branch)
                .FirstOrDefaultAsync(b => b.IdBooking == request.BookingId);
            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đặt bàn." });
            }

            booking.isActive = false;
            booking.IdStatus = request.StatusId ?? 3;

            var availableStatusId = await GetAvailableTableStatusIdAsync();
            if (availableStatusId.HasValue && booking.Table != null)
            {
                booking.Table.IdStatus = availableStatusId.Value;
            }

                var cancellationReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

                if (!string.IsNullOrWhiteSpace(cancellationReason))
            {
                var note = booking.Note ?? string.Empty;
                booking.Note = string.IsNullOrWhiteSpace(note)
                    ? $"Huỷ: {cancellationReason}"
                    : $"{note}\nHuỷ: {cancellationReason}";
            }

            var order = await _context.tblOrder
                .Where(o => o.IdBooking == booking.IdBooking)
                .OrderByDescending(o => o.PaymentTime ?? o.OrderDate)
                .FirstOrDefaultAsync();

            var cancellation = new tblOrder_cancelled
            {
                IdOrder = order?.IdOrder,
                Description = cancellationReason,
                CancelledTime = DateTime.Now
            };

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdValue, out var userId))
            {
                cancellation.CancelledBy = userId;
            }

            _context.tblOrder_cancelled.Add(cancellation);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể huỷ đặt bàn {BookingId}", request.BookingId);
                return Json(new { success = false, message = "Không thể huỷ đặt bàn. Vui lòng thử lại." });
            }

            await _bookingNotificationService.SendBookingCancelledEmailAsync(booking, cancellationReason);

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> History(string tab = "all", int? branchId = null, string? search = null, DateTime? date = null)
        {
            tab = string.IsNullOrWhiteSpace(tab) ? "all" : tab.ToLowerInvariant();
            var targetDate = date?.Date;

            var historyQuery = _context.tblBooking
                .Include(b => b.Customer)
                .Include(b => b.Branch)
                .Include(b => b.Table)
                .Include(b => b.Status)
                .Include(b => b.tblOrder)
                    .ThenInclude(o => o.tblOrder_detail)
                .Include(b => b.tblOrder)
                    .ThenInclude(o => o.Cancellations)
                        .ThenInclude(c => c.CancelledByUser)
                .Where(b => !b.isActive ||
                            (b.IdStatus.HasValue && (b.IdStatus.Value == 3 || b.IdStatus.Value == 4)) ||
                            b.BookingDate.Date < DateTime.Today);

            if (branchId.HasValue)
            {
                historyQuery = historyQuery.Where(b => b.IdBranch == branchId.Value);
            }

            if (targetDate.HasValue)
            {
                historyQuery = historyQuery.Where(b => b.BookingDate.Date == targetDate.Value);
            }

            var bookings = await historyQuery
                .OrderByDescending(b => b.BookingDate)
                .ThenByDescending(b => b.IdBooking)
                .Take(500)
                .ToListAsync();

            var bookingIds = bookings.Select(b => b.IdBooking).ToList();
            var orderLookup = await BuildOrderLookupAsync(bookingIds);

            var cards = bookings.Select(b =>
            {
                var statusId = b.IdStatus ?? 1;
                var statusName = b.Status?.StatusName ?? BookingStatusHelper.GetStatusName(statusId);
                var badgeClass = BookingStatusHelper.GetBadgeClass(statusName);
                var statusKey = BookingStatusHelper.GetStatusKey(statusId);
                var allCancellations = b.tblOrder?
                    .SelectMany(o => o.Cancellations ?? Enumerable.Empty<tblOrder_cancelled>())
                    .ToList() ?? new List<tblOrder_cancelled>();

                var latestCancellation = allCancellations
                    .OrderByDescending(c => c.CancelledTime ?? DateTime.MinValue)
                    .FirstOrDefault();

                string? cancelledBy = null;
                if (latestCancellation?.CancelledByUser != null)
                {
                    cancelledBy = string.Join(' ', new[]
                        {
                            latestCancellation.CancelledByUser.LastName,
                            latestCancellation.CancelledByUser.FirstName
                        }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));

                    if (string.IsNullOrWhiteSpace(cancelledBy))
                    {
                        cancelledBy = latestCancellation.CancelledByUser.UserName;
                    }
                }

                var lastOrder = b.tblOrder?
                    .OrderByDescending(o => o.PaymentTime ?? o.OrderDate)
                    .FirstOrDefault();

                return new BookingCardViewModel
                {
                    Id = b.IdBooking,
                    CustomerName = b.Customer?.FullName ?? "Khách lẻ",
                    CustomerPhone = b.Customer?.PhoneNumber ?? string.Empty,
                    BranchId = b.IdBranch,
                    BranchName = b.Branch?.BranchName,
                    TableName = b.Table?.TableName,
                    TableId = b.IdTable,
                    BookingDate = b.BookingDate,
                    TimeSlot = b.TimeSlot ?? string.Empty,
                    Guests = b.NumberGuests,
                    Note = b.Note,
                    StatusId = statusId,
                    StatusName = statusName,
                    StatusBadgeClass = badgeClass,
                    StatusKey = statusKey,
                    HasOrder = orderLookup.ContainsKey(b.IdBooking),
                    OrderItems = orderLookup.TryGetValue(b.IdBooking, out var items) ? items : new List<BookingOrderItemViewModel>(),
                    CancelledAt = latestCancellation?.CancelledTime,
                    CancellationReason = latestCancellation?.Description,
                    CancelledByName = cancelledBy,
                    LastUpdateTime = lastOrder?.PaymentTime ?? lastOrder?.OrderDate ?? b.BookingDate
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalized = search.Trim().ToLowerInvariant();
                cards = cards.Where(c =>
                        (!string.IsNullOrWhiteSpace(c.CustomerName) && c.CustomerName.ToLowerInvariant().Contains(normalized)) ||
                        (!string.IsNullOrWhiteSpace(c.CustomerPhone) && c.CustomerPhone.Contains(normalized)) ||
                        (!string.IsNullOrWhiteSpace(c.TableName) && c.TableName.ToLowerInvariant().Contains(normalized)) ||
                        (!string.IsNullOrWhiteSpace(c.BranchName) && c.BranchName.ToLowerInvariant().Contains(normalized)) ||
                        (!string.IsNullOrWhiteSpace(c.Note) && c.Note.ToLowerInvariant().Contains(normalized)))
                    .ToList();
            }

            var counters = new Dictionary<string, int>
            {
                ["all"] = cards.Count,
                ["pending"] = cards.Count(c => c.StatusKey == "pending"),
                ["serving"] = cards.Count(c => c.StatusKey == "serving"),
                ["completed"] = cards.Count(c => c.StatusKey == "completed"),
                ["cancelled"] = cards.Count(c => c.StatusKey == "cancelled")
            };

            IEnumerable<BookingCardViewModel> filtered = tab switch
            {
                "pending" => cards.Where(c => c.StatusKey == "pending"),
                "serving" => cards.Where(c => c.StatusKey == "serving"),
                "completed" => cards.Where(c => c.StatusKey == "completed"),
                "cancelled" => cards.Where(c => c.StatusKey == "cancelled"),
                _ => cards
            };

            var branches = await _context.tblBranch
                .Where(b => b.IsActive)
                .OrderBy(b => b.BranchName)
                .Select(b => new SelectOption
                {
                    Id = b.IdBranch,
                    Name = string.IsNullOrWhiteSpace(b.BranchName) ? $"Chi nhánh #{b.IdBranch}" : b.BranchName!
                })
                .ToListAsync();

            var model = new BookingHistoryViewModel
            {
                Bookings = filtered.ToList(),
                ActiveTab = tab,
                TabCounters = counters,
                Branches = branches,
                PageSize = 10,
                SearchTerm = search,
                SelectedBranchId = branchId,
                SelectedDate = targetDate,
                Title = "Lịch sử đặt bàn",
                Description = "Theo dõi các đặt bàn đã phục vụ xong hoặc đã huỷ."
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, string? returnUrl = null)
        {
            var booking = await _context.tblBooking
                .Include(b => b.Customer)
                .Include(b => b.Branch)
                .Include(b => b.Table)
                    .ThenInclude(t => t!.Area)
                .Include(b => b.Table)
                    .ThenInclude(t => t!.Type)
                .Include(b => b.Table)
                    .ThenInclude(t => t!.Status)
                .Include(b => b.Status)
                .Include(b => b.tblOrder)
                    .ThenInclude(o => o.tblOrder_detail)
                        .ThenInclude(d => d.MenuItem)
                .Include(b => b.tblOrder)
                    .ThenInclude(o => o.Cancellations)
                        .ThenInclude(c => c.CancelledByUser)
                .FirstOrDefaultAsync(b => b.IdBooking == id);

            if (booking == null)
            {
                TempData["BookingMessage"] = "Không tìm thấy đặt bàn cần xem.";
                return RedirectToAction(nameof(Index));
            }

            var latestOrder = booking.tblOrder?
                .OrderByDescending(o => o.PaymentTime ?? o.TimeOut ?? o.TimeIn ?? o.OrderDate)
                .FirstOrDefault();

            var orderItems = latestOrder?.tblOrder_detail?
                .Select(d => new BookingOrderItemViewModel
                {
                    Id = d.Id,
                    ItemName = d.MenuItem?.Title ?? $"Món #{d.IdMenuItem}",
                    Quantity = d.Quantity ?? 0,
                    Price = d.PriceSale ?? d.MenuItem?.Price
                })
                .ToList() ?? new List<BookingOrderItemViewModel>();

            var cancellations = booking.tblOrder?
                .SelectMany(o => o.Cancellations ?? Enumerable.Empty<tblOrder_cancelled>())
                .OrderByDescending(c => c.CancelledTime ?? DateTime.MinValue)
                .ToList() ?? new List<tblOrder_cancelled>();

            var latestCancellation = cancellations.FirstOrDefault();
            var latestCancellationBy = GetUserDisplayName(latestCancellation?.CancelledByUser);

            var statusId = booking.IdStatus ?? 1;
            var statusName = booking.Status?.StatusName ?? BookingStatusHelper.GetStatusName(statusId);

            var bookingCard = new BookingCardViewModel
            {
                Id = booking.IdBooking,
                CustomerName = booking.Customer?.FullName ?? "Khách lẻ",
                CustomerPhone = booking.Customer?.PhoneNumber ?? booking.Email ?? string.Empty,
                BranchId = booking.IdBranch,
                BranchName = booking.Branch?.BranchName,
                TableName = booking.Table?.TableName,
                TableId = booking.IdTable,
                BookingDate = booking.BookingDate,
                TimeSlot = booking.TimeSlot ?? string.Empty,
                Guests = booking.NumberGuests,
                Note = booking.Note,
                StatusId = statusId,
                StatusName = statusName,
                StatusBadgeClass = BookingStatusHelper.GetBadgeClass(statusName),
                StatusKey = BookingStatusHelper.GetStatusKey(statusId),
                HasOrder = orderItems.Any(),
                OrderItems = orderItems,
                CancelledAt = latestCancellation?.CancelledTime,
                CancellationReason = latestCancellation?.Description,
                CancelledByName = latestCancellationBy,
                LastUpdateTime = latestOrder?.PaymentTime ?? latestOrder?.TimeOut ?? latestOrder?.TimeIn ?? booking.BookingDate
            };

            var timeline = new List<BookingTimelineEntry>
            {
                new()
                {
                    Title = "Đặt bàn",
                    Description = $"{(booking.NumberGuests ?? 0)} khách · {booking.TimeSlot ?? "Không rõ khung giờ"}",
                    Time = booking.BookingDate,
                    Icon = "bi-calendar-event",
                    StatusClass = "text-primary"
                }
            };

            if (booking.IdTable.HasValue)
            {
                timeline.Add(new BookingTimelineEntry
                {
                    Title = "Xếp bàn",
                    Description = booking.Table?.TableName ?? $"Bàn #{booking.IdTable}",
                    Time = booking.BookingDate,
                    Icon = "bi-diagram-3",
                    StatusClass = "text-info"
                });
            }

            if (latestOrder?.TimeIn.HasValue == true)
            {
                timeline.Add(new BookingTimelineEntry
                {
                    Title = "Khách nhận bàn",
                    Description = $"Vào lúc {latestOrder.TimeIn:HH:mm}",
                    Time = latestOrder.TimeIn,
                    Icon = "bi-person-check",
                    StatusClass = "text-success"
                });
            }

            if (latestOrder?.TimeOut.HasValue == true)
            {
                timeline.Add(new BookingTimelineEntry
                {
                    Title = "Khách trả bàn",
                    Description = $"Rời lúc {latestOrder.TimeOut:HH:mm}",
                    Time = latestOrder.TimeOut,
                    Icon = "bi-door-open",
                    StatusClass = "text-muted"
                });
            }

            if (latestOrder?.PaymentTime.HasValue == true)
            {
                timeline.Add(new BookingTimelineEntry
                {
                    Title = "Hoàn tất thanh toán",
                    Description = latestOrder.TotalAmount.HasValue
                        ? $"Đã thu {latestOrder.TotalAmount.Value:N0} ₫"
                        : "Đã ghi nhận thanh toán",
                    Time = latestOrder.PaymentTime,
                    Icon = "bi-credit-card",
                    StatusClass = "text-success"
                });
            }

            foreach (var cancellation in cancellations)
            {
                timeline.Add(new BookingTimelineEntry
                {
                    Title = "Huỷ đặt bàn",
                    Description = string.IsNullOrWhiteSpace(cancellation.Description)
                        ? "Không ghi nhận lý do"
                        : cancellation.Description,
                    Time = cancellation.CancelledTime,
                    Icon = "bi-x-circle",
                    StatusClass = "text-danger"
                });
            }

            timeline = timeline
                .OrderBy(t => t.Time ?? DateTime.MinValue)
                .ToList();

            var payment = new BookingPaymentSummary
            {
                OrderId = latestOrder?.IdOrder,
                Subtotal = orderItems.Sum(i => (i.Price ?? 0) * i.Quantity),
                PaymentMethod = latestOrder?.PaymentMethod,
                PaymentTime = latestOrder?.PaymentTime,
                TimeIn = latestOrder?.TimeIn,
                TimeOut = latestOrder?.TimeOut,
                Notes = latestOrder?.Notes,
                AmountPaid = latestOrder?.TotalAmount
            };

            var customerSnapshot = new BookingCustomerSnapshot
            {
                Name = booking.Customer?.FullName ?? "Khách lẻ",
                Phone = booking.Customer?.PhoneNumber ?? booking.Email,
                Email = booking.Customer?.Email ?? booking.Email,
                Address = booking.Customer?.Address
            };

            if (booking.IdCustomer.HasValue)
            {
                customerSnapshot.TotalBookings = await _context.tblBooking
                    .CountAsync(b => b.IdCustomer == booking.IdCustomer);

                customerSnapshot.LastBookingDate = await _context.tblBooking
                    .Where(b => b.IdCustomer == booking.IdCustomer && b.IdBooking != booking.IdBooking)
                    .OrderByDescending(b => b.BookingDate)
                    .Select(b => (DateTime?)b.BookingDate)
                    .FirstOrDefaultAsync();
            }

            var branchSnapshot = new BookingBranchSnapshot
            {
                Id = booking.IdBranch,
                Name = booking.Branch?.BranchName,
                Address = booking.Branch?.Address,
                Phone = booking.Branch?.PhoneNumber,
                Email = booking.Branch?.Email
            };

            var tableSnapshot = new BookingTableSnapshot
            {
                Id = booking.Table?.IdTable,
                Name = booking.Table?.TableName,
                Area = booking.Table?.Area?.AreaName,
                Type = booking.Table?.Type?.TypeName,
                MaxSeats = booking.Table?.Type?.MaxSeats,
                StatusName = booking.Table?.Status?.StatusName
            };

            var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Index));

            var viewModel = new BookingDetailViewModel
            {
                Booking = bookingCard,
                Customer = customerSnapshot,
                Branch = branchSnapshot,
                Table = tableSnapshot,
                OrderItems = orderItems,
                Payment = payment,
                Timeline = timeline,
                Cancellations = cancellations
                    .Select(c => new BookingCancellationViewModel
                    {
                        CancelledAt = c.CancelledTime,
                        Reason = c.Description,
                        CancelledBy = GetUserDisplayName(c.CancelledByUser)
                    })
                    .ToList(),
                ReturnUrl = safeReturnUrl
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> AvailableTables(DateTime date, string timeSlot, int? branchId, int? bookingId, int? minSeats)
        {
            var normalizedTimeSlot = (timeSlot ?? string.Empty).Trim();
            var tablesQuery = _context.tblTables
                .Include(t => t.Area)
                .Include(t => t.Type)
                .Where(t => t.isActive);

            if (branchId.HasValue)
            {
                tablesQuery = tablesQuery.Where(t => t.Area != null && t.Area.IdBranch == branchId);
            }

            if (minSeats.HasValue)
            {
                var requiredSeats = minSeats.Value;
                tablesQuery = tablesQuery.Where(t =>
                    t.Type == null ||
                    !t.Type.MaxSeats.HasValue ||
                    t.Type.MaxSeats.Value >= requiredSeats);
            }

            var availableStatusId = await GetAvailableTableStatusIdAsync();
            if (availableStatusId.HasValue)
            {
                tablesQuery = tablesQuery.Where(t => !t.IdStatus.HasValue || t.IdStatus == availableStatusId.Value);
            }
            else
            {
                tablesQuery = tablesQuery.Where(t => !t.IdStatus.HasValue);
            }

            var tables = await tablesQuery.ToListAsync();
            var result = new List<object>();

            foreach (var table in tables)
            {
                if (await IsTableAvailableAsync(table.IdTable, date.Date, normalizedTimeSlot, bookingId))
                {
                    result.Add(new
                    {
                        id = table.IdTable,
                        name = table.TableName ?? $"Bàn #{table.IdTable}",
                        area = table.Area?.AreaName,
                        branchId = table.Area?.IdBranch,
                        seats = table.Type?.MaxSeats
                    });
                }
            }

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> CurrentOrder(int bookingId)
        {
            var booking = await _context.tblBooking
                .Include(b => b.Customer)
                .Include(b => b.Branch)
                .Include(b => b.Table)
                    .ThenInclude(t => t!.Type)
                .Include(b => b.Status)
                .FirstOrDefaultAsync(b => b.IdBooking == bookingId);

            if (booking == null)
            {
                TempData["BookingMessage"] = "Không tìm thấy đặt bàn.";
                return RedirectToAction(nameof(Index));
            }

            var order = await _context.tblOrder
                .Include(o => o.tblOrder_detail)
                .FirstOrDefaultAsync(o => o.IdBooking == booking.IdBooking);

            if (order == null)
            {
                order = new tblOrder
                {
                    IdBooking = booking.IdBooking,
                    IdCustomer = booking.IdCustomer,
                    IdTable = booking.IdTable,
                    OrderDate = DateTime.Now,
                    TimeIn = DateTime.Now
                };
                _context.tblOrder.Add(order);
                await _context.SaveChangesAsync();
            }
            else if (!order.TimeIn.HasValue)
            {
                order.TimeIn = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            var orderLookup = await BuildOrderLookupAsync(new List<int> { booking.IdBooking });
            var orderItems = orderLookup.TryGetValue(booking.IdBooking, out var items)
                ? items
                : new List<BookingOrderItemViewModel>();

            var currentStatusId = booking.IdStatus ?? 1;
            var currentStatusName = booking.Status?.StatusName ?? BookingStatusHelper.GetStatusName(currentStatusId);

            var bookingCard = new BookingCardViewModel
            {
                Id = booking.IdBooking,
                CustomerName = booking.Customer?.FullName ?? "Khách lẻ",
                CustomerPhone = booking.Customer?.PhoneNumber ?? string.Empty,
                BranchId = booking.IdBranch,
                BranchName = booking.Branch?.BranchName,
                TableName = booking.Table?.TableName,
                TableId = booking.IdTable,
                BookingDate = booking.BookingDate,
                TimeSlot = booking.TimeSlot ?? string.Empty,
                Guests = booking.NumberGuests,
                Note = booking.Note,
                StatusId = currentStatusId,
                StatusName = currentStatusName,
                StatusBadgeClass = BookingStatusHelper.GetBadgeClass(currentStatusName),
                HasOrder = orderItems.Any(),
                OrderItems = orderItems
            };

            var menuItems = await _context.tblMenuItem
                .Where(m => m.IsActive)
                .OrderBy(m => m.Title)
                .Select(m => new SelectOption
                {
                    Id = m.IdMenuItem,
                    Name = m.Title ?? $"Món #{m.IdMenuItem}"
                })
                .ToListAsync();

            var viewModel = new BookingInProgressViewModel
            {
                Booking = bookingCard,
                MenuItems = menuItems,
                ReturnUrl = Url.Action(nameof(Index), new { date = booking.BookingDate.ToString("yyyy-MM-dd"), branchId = booking.IdBranch })
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Invoice(int bookingId, bool autoPrint = false)
        {
            var order = await _context.tblOrder
                .Include(o => o.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(o => o.Booking)
                    .ThenInclude(b => b.Branch)
                .Include(o => o.Booking)
                    .ThenInclude(b => b.Table)
                .Include(o => o.Booking)
                    .ThenInclude(b => b.Status)
                .Include(o => o.tblOrder_detail)
                    .ThenInclude(d => d.MenuItem)
                .FirstOrDefaultAsync(o => o.IdBooking == bookingId);

            if (order?.Booking == null)
            {
                TempData["BookingMessage"] = "Không tìm thấy hoá đơn cần in.";
                return RedirectToAction(nameof(Index));
            }

            var booking = order.Booking;
            var orderItems = order.tblOrder_detail
                .Select(d => new BookingOrderItemViewModel
                {
                    Id = d.Id,
                    ItemName = d.MenuItem?.Title ?? "Món",
                    Quantity = d.Quantity ?? 0,
                    Price = d.PriceSale ?? d.MenuItem?.Price
                })
                .ToList();

            var invoiceStatusId = booking.IdStatus ?? 1;
            var invoiceStatusName = booking.Status?.StatusName ?? BookingStatusHelper.GetStatusName(invoiceStatusId);

            var bookingCard = new BookingCardViewModel
            {
                Id = booking.IdBooking,
                CustomerName = booking.Customer?.FullName ?? "Khách lẻ",
                CustomerPhone = booking.Customer?.PhoneNumber ?? string.Empty,
                BranchId = booking.IdBranch,
                BranchName = booking.Branch?.BranchName,
                TableName = booking.Table?.TableName,
                TableId = booking.IdTable,
                BookingDate = booking.BookingDate,
                TimeSlot = booking.TimeSlot ?? string.Empty,
                Guests = booking.NumberGuests,
                Note = booking.Note,
                StatusId = invoiceStatusId,
                StatusName = invoiceStatusName,
                StatusBadgeClass = BookingStatusHelper.GetBadgeClass(invoiceStatusName),
                HasOrder = orderItems.Any(),
                OrderItems = orderItems
            };

            var viewModel = new BookingInvoiceViewModel
            {
                Booking = bookingCard,
                OrderItems = orderItems,
                OrderTotal = orderItems.Sum(i => (i.Price ?? 0) * i.Quantity),
                PaymentMethod = order.PaymentMethod,
                PaymentTime = order.PaymentTime,
                TimeIn = order.TimeIn,
                TimeOut = order.TimeOut
            };

            ViewBag.AutoPrint = autoPrint;

            return View(viewModel);
        }

        private static string? GetUserDisplayName(tblUser? user)
        {
            if (user == null)
            {
                return null;
            }

            var parts = new[] { user.LastName, user.FirstName }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToList();

            if (parts.Any())
            {
                return string.Join(" ", parts);
            }

            return string.IsNullOrWhiteSpace(user.UserName) ? null : user.UserName;
        }

        private async Task<int> CalculateOrderTotalAsync(int bookingId)
        {
            var total = await _context.tblOrder_detail
                .Include(d => d.Order)
                .Where(d => d.Order.IdBooking == bookingId)
                .SumAsync(d => (int?)d.Amount);

            return total ?? 0;
        }

        private async Task<Dictionary<int, List<BookingOrderItemViewModel>>> BuildOrderLookupAsync(List<int> bookingIds)
        {
            var lookup = new Dictionary<int, List<BookingOrderItemViewModel>>();
            if (!bookingIds.Any())
            {
                return lookup;
            }

            var orderItems = await _context.tblOrder_detail
                .Include(o => o.Order)
                .Include(o => o.MenuItem)
                .Where(o => o.Order.IdBooking.HasValue && bookingIds.Contains(o.Order.IdBooking.Value))
                .ToListAsync();

            foreach (var item in orderItems)
            {
                var bookingId = item.Order.IdBooking!.Value;
                if (!lookup.TryGetValue(bookingId, out var list))
                {
                    list = new List<BookingOrderItemViewModel>();
                    lookup[bookingId] = list;
                }

                list.Add(new BookingOrderItemViewModel
                {
                    Id = item.Id,
                    ItemName = item.MenuItem?.Title ?? "Món",
                    Quantity = item.Quantity ?? 0,
                    Price = item.PriceSale
                });
            }

            return lookup;
        }

        private Task<int?> GetDefaultBookingStatusIdAsync() => Task.FromResult<int?>(1);

        private async Task<int?> GetReservedTableStatusIdAsync()
        {
            if (_reservedTableStatusId.HasValue)
            {
                return _reservedTableStatusId;
            }

            var reservedId = await _context.tblTable_status
                .Where(s => s.isActive && (s.StatusName ?? string.Empty).ToLower().Contains("đã đặt"))
                .Select(s => (int?)s.IdStatus)
                .FirstOrDefaultAsync();

            _reservedTableStatusId = reservedId;
            return _reservedTableStatusId;
        }

        private async Task<int?> GetAvailableTableStatusIdAsync()
        {
            if (_availableTableStatusId.HasValue)
            {
                return _availableTableStatusId;
            }

            var availableId = await _context.tblTable_status
                .Where(s => s.isActive && (s.StatusName ?? string.Empty).ToLower().Contains("trống"))
                .Select(s => (int?)s.IdStatus)
                .FirstOrDefaultAsync();

            _availableTableStatusId = availableId;
            return _availableTableStatusId;
        }

        private async Task<int?> GetServingTableStatusIdAsync()
        {
            if (_servingTableStatusId.HasValue)
            {
                return _servingTableStatusId;
            }

            var servingId = await _context.tblTable_status
                .Where(s => s.isActive && (s.StatusName ?? string.Empty).ToLower().Contains("phục vụ"))
                .Select(s => (int?)s.IdStatus)
                .FirstOrDefaultAsync();

            _servingTableStatusId = servingId;
            return _servingTableStatusId;
        }

        private Task<int?> GetServingBookingStatusIdAsync()
        {
            _servingBookingStatusId ??= 2;
            return Task.FromResult(_servingBookingStatusId);
        }

        private async Task<bool> IsTableAvailableAsync(int tableId, DateTime date, string timeSlot, int? ignoreBookingId)
        {
            var normalizedSlot = (timeSlot ?? string.Empty).Trim();
            return !await _context.tblBooking.AnyAsync(b =>
                b.IdTable == tableId &&
                b.isActive &&
                b.BookingDate.Date == date.Date &&
                ((b.TimeSlot ?? string.Empty).Trim() == normalizedSlot) &&
                (!ignoreBookingId.HasValue || b.IdBooking != ignoreBookingId.Value));
        }
    }
}
