using Microsoft.AspNetCore.Mvc;
using KD_Restaurant.Models;
using KD_Restaurant.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KD_Restaurant.Controllers
{
    public class CartController : Controller
    {
        private readonly KDContext _context;
        public CartController(KDContext context) { _context = context; }

        // Hiển thị giỏ hàng
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var branches = _context.tblBranch.ToList(); // Lấy danh sách chi nhánh
            ViewBag.Branches = branches;                // Gán vào ViewBag
            return View(cart);
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
        public IActionResult PlaceOrder(
            int IdBranch, 
            DateTime BookingDate,
            string TimeSlot,
            int NumberGuests,
            string Note,
            string FullName,
            string PhoneNumber
        )
        
        {
            var customer = _context.tblCustomer.FirstOrDefault(x => x.PhoneNumber == PhoneNumber);
            if (customer == null)
            {
                customer = new tblCustomer
                {
                    FullName = FullName,
                    PhoneNumber = PhoneNumber
                    // ... các trường khác nếu có ...
                };
                _context.tblCustomer.Add(customer);
                _context.SaveChanges();
            }
            var booking = new tblBooking
            {
                IdBranch = IdBranch,
                BookingDate = BookingDate,
                TimeSlot = TimeSlot,
                NumberGuests = NumberGuests,
                Note = Note,
                isActive = true,
                IdCustomer = customer.IdCustomer
            };
            _context.tblBooking.Add(booking);
            _context.SaveChanges();

            var order = new tblOrder
            {
                IdBooking = booking.IdBooking,
                
                OrderDate = DateTime.Now,
                TotalAmount = 0, // Sẽ tính sau
            };
            _context.tblOrder.Add(order);
            _context.SaveChanges();

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            int total = 0;
            foreach (var item in cart)
            {
                var detail = new tblOrder_detail
                {
                    IdOrder = order.IdOrder,
                    IdMenuItem = item.IdMenuItem,
                    Quantity = item.Quantity,
                    PriceSale = (int?)item.Price
                };
            total += item.Price * (item.Quantity);
            _context.tblOrder_detail.Add(detail);
            }
            order.TotalAmount = total;
            _context.SaveChanges();
            
        
             // Sau khi lưu xong, chuyển hướng sang diotrang thanh toán
            return RedirectToAction("Checkout", new { id = order.IdOrder });
        }

        public IActionResult Checkout(int id)
        {
            // Lấy thông tin order, booking, customer, order_detail theo id
            var order = _context.tblOrder
                .Include(o => o.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(o => o.Booking)
                    .ThenInclude(b => b.Branch) // Thêm dòng này để lấy thông tin chi nhánh!
                .FirstOrDefault(o => o.IdOrder == id);
        
            var orderDetails = _context.tblOrder_detail
                .Where(d => d.IdOrder == id)
                .Include(d => d.MenuItem)
                .ToList();
        
            if (order == null) return NotFound();
        
            // Truyền dữ liệu sang view
            ViewBag.Order = order;
            ViewBag.OrderDetails = orderDetails;
            return View();
        }
        [HttpPost]
        public IActionResult ConfirmPayment(int id, string paymentMethod)
        {
            var order = _context.tblOrder.FirstOrDefault(o => o.IdOrder == id);
            if (order == null) return NotFound();
        
            order.PaymentMethod = paymentMethod;
            order.Status = 1; // Đã đặt bàn thành công (tùy bạn quy ước)
            order.PaymentTime = DateTime.Now;
            _context.SaveChanges();
        
            TempData["SuccessMessage"] = "Đặt bàn thành công! Cảm ơn bạn đã sử dụng dịch vụ.";
            return RedirectToAction("Success");
        }
        
        // Trang thông báo thành công
        public IActionResult Success()
        {
            return View();
        }
        
    }
}