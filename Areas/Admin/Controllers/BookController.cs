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

namespace KD_Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.AdminManagerStaff)]
    [PermissionAuthorize(PermissionKeys.OrderManagement)]
    public class BookController : Controller
    {
        private readonly KDContext _context;
        private int? _servingTableStatusId;

        public BookController(KDContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? highlightBookingId = null)
        {
            var servingStatusId = await _context.tblBooking_status
                .Where(s => s.isActive && (s.StatusName ?? string.Empty).ToLower().Contains("phục vụ"))
                .Select(s => (int?)s.IdStatus)
                .FirstOrDefaultAsync();

            const int PendingStatusId = 1;
            var activeServingStatusId = servingStatusId ?? 2;

            var bookingsQuery = _context.tblBooking
                .Include(b => b.Customer)
                .Include(b => b.Table)
                .Include(b => b.Status)
                .Include(b => b.tblOrder)
                    .ThenInclude(o => o.tblOrder_detail)
                        .ThenInclude(d => d.MenuItem)
                .Include(b => b.tblOrder)
                    .ThenInclude(o => o.Cancellations)
                        .ThenInclude(c => c.CancelledByUser)
                .Where(b => b.isActive)
                .Where(b =>
                    b.tblOrder.Any(o => o.PaymentTime == null) ||
                    !b.IdStatus.HasValue ||
                    b.IdStatus == PendingStatusId ||
                    b.IdStatus == activeServingStatusId);

            var bookings = await bookingsQuery.ToListAsync();

            var orders = bookings
                .Select(b =>
                {
                    var currentOrder = b.tblOrder
                        .Where(o => o.PaymentTime == null)
                        .OrderByDescending(o => o.OrderDate)
                        .FirstOrDefault();
                    var lastActivity = currentOrder?.OrderDate ?? b.BookingDate;
                    return new
                    {
                        Summary = OrderSummaryHelper.FromBooking(b, currentOrder),
                        LastActivity = lastActivity
                    };
                })
                .OrderByDescending(x => x.LastActivity)
                .Select(x => x.Summary)
                .ToList();

            var branches = await GetActiveBranchesAsync();

            var model = new CurrentOrderListViewModel
            {
                Orders = orders,
                HighlightBookingId = highlightBookingId,
                Branches = branches
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> History(string tab = "all", string? search = null)
        {
            tab = string.IsNullOrWhiteSpace(tab) ? "all" : tab.ToLowerInvariant();

            var bookingsQuery = _context.tblBooking
                .Include(b => b.Customer)
                .Include(b => b.Branch)
                .Include(b => b.Table)
                .Include(b => b.Status)
                .Include(b => b.tblOrder)
                    .ThenInclude(o => o.tblOrder_detail)
                        .ThenInclude(d => d.MenuItem)
                .Include(b => b.tblOrder)
                    .ThenInclude(o => o.Cancellations)
                        .ThenInclude(c => c.CancelledByUser)
                .Include(b => b.tblOrder)
                    .ThenInclude(o => o.User)
                .Where(b => !b.isActive ||
                            (b.IdStatus.HasValue && (b.IdStatus.Value == 3 || b.IdStatus.Value == 4)) ||
                            b.tblOrder.Any(o => o.PaymentTime != null));

            var bookings = await bookingsQuery
                .OrderByDescending(b => b.BookingDate)
                .ThenByDescending(b => b.IdBooking)
                .Take(400)
                .ToListAsync();

            var summaries = bookings.Select(b =>
            {
                var currentOrder = b.tblOrder?
                    .OrderByDescending(o => o.PaymentTime ?? o.OrderDate)
                    .FirstOrDefault();
                return OrderSummaryHelper.FromBooking(b, currentOrder);
            }).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalized = search.Trim().ToLowerInvariant();
                summaries = summaries
                    .Where(s =>
                        (!string.IsNullOrWhiteSpace(s.CustomerName) && s.CustomerName.ToLowerInvariant().Contains(normalized)) ||
                        (!string.IsNullOrWhiteSpace(s.CustomerPhone) && s.CustomerPhone.Contains(normalized)) ||
                        (!string.IsNullOrWhiteSpace(s.TableName) && s.TableName.ToLowerInvariant().Contains(normalized)) ||
                        (!string.IsNullOrWhiteSpace(s.ReferenceCode) && s.ReferenceCode.ToLowerInvariant().Contains(normalized)))
                    .ToList();
            }

            var counters = new Dictionary<string, int>
            {
                ["all"] = summaries.Count,
                ["pending"] = summaries.Count(s => s.StatusKey == "pending"),
                ["paid"] = summaries.Count(s => s.StatusKey == "paid"),
                ["cancelled"] = summaries.Count(s => s.StatusKey == "cancelled")
            };

            IEnumerable<CurrentOrderSummaryViewModel> filtered = tab switch
            {
                "pending" => summaries.Where(s => s.StatusKey == "pending"),
                "paid" => summaries.Where(s => s.StatusKey == "paid"),
                "cancelled" => summaries.Where(s => s.StatusKey == "cancelled"),
                _ => summaries
            };

            var model = new CurrentOrderListViewModel
            {
                Orders = filtered.ToList(),
                IsHistory = true,
                ActiveTab = tab,
                TabCounters = counters,
                SearchTerm = search,
                CustomTitle = "Lịch sử hoá đơn",
                CustomDescription = "Theo dõi các hoá đơn đã thanh toán, chờ xác nhận hoặc đã huỷ.",
                PageSize = 10
            };

            return View("History", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWalkIn(WalkInCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                            ?? "Thông tin không hợp lệ.";
                return Json(new { success = false, message = error });
            }

            var branch = await _context.tblBranch
                .FirstOrDefaultAsync(b => b.IdBranch == request.BranchId && b.IsActive);

            if (branch == null)
            {
                return Json(new { success = false, message = "Chi nhánh không hợp lệ." });
            }

            var now = DateTime.Now;
            var normalizedSlot = now.ToString("HH:mm");

            tblCustomer? customer = null;
            if (!string.IsNullOrWhiteSpace(request.CustomerPhone))
            {
                customer = await _context.tblCustomer
                    .FirstOrDefaultAsync(c => c.PhoneNumber == request.CustomerPhone);
            }

            if (customer == null && (!string.IsNullOrWhiteSpace(request.CustomerName) || !string.IsNullOrWhiteSpace(request.CustomerPhone)))
            {
                customer = new tblCustomer
                {
                    FullName = string.IsNullOrWhiteSpace(request.CustomerName) ? "Khách vãng lai" : request.CustomerName,
                    PhoneNumber = request.CustomerPhone,
                    IsActive = true
                };
                _context.tblCustomer.Add(customer);
            }

            tblTable? table = null;
            if (request.TableId.HasValue)
            {
                table = await _context.tblTables
                    .Include(t => t.Area)
                    .FirstOrDefaultAsync(t => t.IdTable == request.TableId.Value && t.isActive);

                if (table == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bàn phục vụ đã chọn." });
                }

                var available = await IsTableAvailableAsync(table.IdTable, now.Date, normalizedSlot, null);
                if (!available)
                {
                    return Json(new { success = false, message = "Bàn đang được sử dụng trong thời gian này." });
                }
            }

            var booking = new tblBooking
            {
                BookingDate = now.Date,
                TimeSlot = normalizedSlot,
                NumberGuests = request.Guests,
                Note = string.IsNullOrWhiteSpace(request.Note) ? "Khách vãng lai" : request.Note,
                isActive = true,
                IdStatus = 2,
                IdBranch = branch.IdBranch
            };

            var order = new tblOrder
            {
                Booking = booking,
                OrderDate = now,
                TimeIn = now,
                Status = 1,
                Notes = booking.Note
            };

            if (customer != null)
            {
                if (_context.Entry(customer).State == EntityState.Added)
                {
                    booking.Customer = customer;
                    order.Customer = customer;
                }
                else
                {
                    booking.IdCustomer = customer.IdCustomer;
                    order.IdCustomer = customer.IdCustomer;
                }
            }

            if (table != null)
            {
                booking.Table = table;
                booking.IdTable = table.IdTable;
                order.IdTable = table.IdTable;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userId, out var idUser))
            {
                order.IdUser = idUser;
            }

            booking.tblOrder.Add(order);

            _context.tblBooking.Add(booking);
            _context.tblOrder.Add(order);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Json(new { success = false, message = "Không thể tạo hoá đơn trực tiếp." });
            }

            return Json(new
            {
                success = true,
                redirectUrl = Url.Action("CurrentOrder", "Bookings", new { area = "Admin", bookingId = booking.IdBooking })
            });
        }

        private async Task<List<SelectOption>> GetActiveBranchesAsync()
        {
            return await _context.tblBranch
                .Where(b => b.IsActive)
                .OrderBy(b => b.BranchName)
                .Select(b => new SelectOption
                {
                    Id = b.IdBranch,
                    Name = string.IsNullOrWhiteSpace(b.BranchName) ? $"Chi nhánh #{b.IdBranch}" : b.BranchName!
                })
                .ToListAsync();
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
