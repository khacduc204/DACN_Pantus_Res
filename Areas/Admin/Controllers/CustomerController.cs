using System;
using System.Data.SqlTypes;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using KD_Restaurant.Models;
using KD_Restaurant.Security;
using KD_Restaurant.Utilities;
using KD_Restaurant.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KD_Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.AdminManagerStaff)]
    [PermissionAuthorize(PermissionKeys.CustomerManagement)]
    public class CustomerController : Controller
    {
        private readonly KDContext _context;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(KDContext context, ILogger<CustomerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, string status = "all", int page = 1, int pageSize = 10)
        {
            status = string.IsNullOrWhiteSpace(status) ? "all" : status.ToLowerInvariant();
            pageSize = Math.Clamp(pageSize, 5, 50);
            page = Math.Max(1, page);

            var query = _context.tblCustomer
                .Include(c => c.User)
                .Include(c => c.MembershipCard)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(c =>
                    (c.FullName != null && c.FullName.Contains(keyword)) ||
                    (c.PhoneNumber != null && c.PhoneNumber.Contains(keyword)) ||
                    (c.Address != null && c.Address.Contains(keyword)));
            }

            query = status switch
            {
                "active" => query.Where(c => c.IsActive),
                "inactive" => query.Where(c => !c.IsActive),
                _ => query
            };

            var totalFiltered = await query.CountAsync();

            var customers = await query
                .OrderByDescending(c => c.LastLogin ?? SqlDateTime.MinValue.Value)
                .ThenBy(c => c.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerListItemViewModel
                {
                    Id = c.IdCustomer,
                    DisplayName = !string.IsNullOrWhiteSpace(c.FullName)
                        ? c.FullName!
                        : (c.User != null && !string.IsNullOrWhiteSpace(c.User.UserName)
                            ? c.User.UserName!
                            : $"Khách #{c.IdCustomer}"),
                    PhoneNumber = c.PhoneNumber,
                    Email = c.Email,
                    Address = c.Address,
                    LastLogin = c.LastLogin,
                    BookingCount = c.tblBooking.Count(),
                    IsActive = c.IsActive,
                    AccountUserName = c.User != null ? c.User.UserName : null,
                    HasMembershipCard = c.MembershipCard != null,
                    MembershipCardNumber = c.MembershipCard != null ? c.MembershipCard.CardNumber : null,
                    MembershipPoints = c.MembershipCard != null ? c.MembershipCard.Points : 0,
                    MembershipCreatedDate = c.MembershipCard != null ? c.MembershipCard.CreatedDate : null
                })
                .ToListAsync();

            var weekAgo = DateTime.UtcNow.AddDays(-7);

            var viewModel = new CustomerManagementViewModel
            {
                Customers = customers,
                SearchTerm = search,
                StatusFilter = status,
                TotalCustomers = await _context.tblCustomer.CountAsync(),
                ActiveCustomers = await _context.tblCustomer.CountAsync(c => c.IsActive),
                NewCustomersThisWeek = await _context.tblCustomer.CountAsync(c => c.LastLogin != null && c.LastLogin >= weekAgo),
                CurrentPage = page,
                PageSize = pageSize,
                TotalFilteredCustomers = totalFiltered
            };

            return View(viewModel);
        }

        public IActionResult Create()
        {
            return View(new CustomerFormModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerFormModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var entity = new tblCustomer
            {
                FullName = model.FullName.Trim(),
                PhoneNumber = NormalizeString(model.PhoneNumber),
                Email = NormalizeEmail(model.Email),
                Address = NormalizeString(model.Address),
                Avatar = NormalizeString(model.Avatar),
                IsActive = model.IsActive,
                LastLogin = DateTime.UtcNow
            };

            _context.tblCustomer.Add(entity);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm khách hàng mới.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _context.tblCustomer.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            var form = new CustomerFormModel
            {
                Id = customer.IdCustomer,
                FullName = customer.FullName ?? string.Empty,
                PhoneNumber = customer.PhoneNumber,
                Email = customer.Email,
                Address = customer.Address,
                Avatar = customer.Avatar,
                IsActive = customer.IsActive
            };

            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerFormModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = await _context.tblCustomer.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            customer.FullName = model.FullName.Trim();
            customer.PhoneNumber = NormalizeString(model.PhoneNumber);
            customer.Email = NormalizeEmail(model.Email);
            customer.Address = NormalizeString(model.Address);
            customer.Avatar = NormalizeString(model.Avatar);
            customer.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật thông tin khách hàng.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var customer = await _context.tblCustomer
                .Include(c => c.User)
                .Include(c => c.MembershipCard)
                .FirstOrDefaultAsync(c => c.IdCustomer == id);
            if (customer == null)
            {
                return NotFound();
            }

            var bookings = await _context.tblBooking
                .Include(b => b.Status)
                .Where(b => b.IdCustomer == id)
                .OrderByDescending(b => b.BookingDate)
                .ThenByDescending(b => b.IdBooking)
                .Take(5)
                .Select(b => new CustomerTimelineItem
                {
                    BookingId = b.IdBooking,
                    BookingDate = b.BookingDate,
                    TimeSlot = b.TimeSlot,
                    Status = b.Status != null ? b.Status.StatusName : null,
                    NumberGuests = b.NumberGuests
                })
                .ToListAsync();

            var detailModel = new CustomerDetailsViewModel
            {
                Id = customer.IdCustomer,
                DisplayName = !string.IsNullOrWhiteSpace(customer.FullName)
                    ? customer.FullName!
                    : (!string.IsNullOrWhiteSpace(customer.User?.UserName) ? customer.User!.UserName : $"Khách #{customer.IdCustomer}"),
                AccountUserName = customer.User?.UserName,
                PhoneNumber = customer.PhoneNumber,
                Email = customer.Email,
                Address = customer.Address,
                Avatar = customer.Avatar,
                LastLogin = customer.LastLogin,
                IsActive = customer.IsActive,
                BookingCount = await _context.tblBooking.CountAsync(b => b.IdCustomer == id),
                OrderCount = await _context.tblOrder.CountAsync(o => o.IdCustomer == id),
                RecentBookings = bookings
            };

            if (customer.MembershipCard != null)
            {
                var card = customer.MembershipCard;
                detailModel.HasMembershipCard = true;
                detailModel.MembershipCardNumber = card.CardNumber;
                detailModel.MembershipPoints = card.Points;
                detailModel.MembershipStatus = card.Status;
                detailModel.MembershipCreatedDate = card.CreatedDate;

                var history = await _context.tblPointHistory
                    .Where(p => p.IdCard == card.IdCard)
                    .OrderByDescending(p => p.CreatedDate)
                    .Take(10)
                    .Select(p => new CustomerPointHistoryItem
                    {
                        CreatedDate = p.CreatedDate,
                        ChangeType = p.ChangeType,
                        Points = p.Points,
                        ReferenceId = p.ReferenceId
                    })
                    .ToListAsync();

                detailModel.MembershipHistory = history;
                detailModel.LastPointActivity = history.FirstOrDefault()?.CreatedDate;
            }

            return View(detailModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var customer = await _context.tblCustomer.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            customer.IsActive = !customer.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = customer.IsActive
                ? "Đã kích hoạt lại khách hàng."
                : "Đã tạm khoá khách hàng.";

            return RedirectToAction(nameof(Index));
        }

        private static string? NormalizeEmail(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            try
            {
                var address = new MailAddress(trimmed);
                return address.Address.ToLowerInvariant();
            }
            catch
            {
                return trimmed.ToLowerInvariant();
            }
        }

        private static string? NormalizeString(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
