using System;
using System.Linq;
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
    [Authorize(Roles = RoleNames.AdminOrManager)]
    [PermissionAuthorize(PermissionKeys.BranchManagement)]
    public class BranchController : Controller
    {
        private readonly KDContext _context;
        private readonly ILogger<BranchController> _logger;

        public BranchController(KDContext context, ILogger<BranchController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            var branches = await _context.tblBranch
                .AsNoTracking()
                .Select(b => new BranchListItemViewModel
                {
                    Id = b.IdBranch,
                    BranchName = string.IsNullOrEmpty(b.BranchName)
                        ? "Cơ sở #" + b.IdBranch
                        : b.BranchName!,
                    Address = b.Address,
                    PhoneNumber = b.PhoneNumber,
                    Email = b.Email,
                    Description = b.Description,
                    IsActive = b.IsActive,
                    AreaCount = b.Areas.Count(),
                    TableCount = b.Areas.SelectMany(a => a.Tables).Count(),
                    UpcomingBookings = b.tblBooking.Count(x => x.BookingDate >= today),
                    LastBookingDate = b.tblBooking
                        .OrderByDescending(x => x.BookingDate)
                        .Select(x => (DateTime?)x.BookingDate)
                        .FirstOrDefault()
                })
                .OrderBy(b => b.BranchName)
                .ToListAsync();

            var model = new BranchDashboardViewModel
            {
                Branches = branches,
                TotalBranches = branches.Count,
                ActiveBranches = branches.Count(b => b.IsActive),
                TotalAreas = branches.Sum(b => b.AreaCount),
                UpcomingBookings = branches.Sum(b => b.UpcomingBookings)
            };

            return View(model);
        }

        public IActionResult Create()
        {
            return View(new BranchFormModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BranchFormModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var entity = new tblBranch
            {
                BranchName = model.BranchName.Trim(),
                Address = Normalize(model.Address),
                PhoneNumber = Normalize(model.PhoneNumber),
                Email = Normalize(model.Email),
                Description = Normalize(model.Description),
                IsActive = model.IsActive
            };

            _context.tblBranch.Add(entity);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm cơ sở mới.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var branch = await _context.tblBranch.FindAsync(id);
            if (branch == null)
            {
                return NotFound();
            }

            var form = new BranchFormModel
            {
                Id = branch.IdBranch,
                BranchName = branch.BranchName ?? string.Empty,
                Address = branch.Address,
                PhoneNumber = branch.PhoneNumber,
                Email = branch.Email,
                Description = branch.Description,
                IsActive = branch.IsActive
            };

            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BranchFormModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var branch = await _context.tblBranch.FindAsync(id);
            if (branch == null)
            {
                return NotFound();
            }

            branch.BranchName = model.BranchName.Trim();
            branch.Address = Normalize(model.Address);
            branch.PhoneNumber = Normalize(model.PhoneNumber);
            branch.Email = Normalize(model.Email);
            branch.Description = Normalize(model.Description);
            branch.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật thông tin cơ sở.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var branch = await _context.tblBranch.FindAsync(id);
            if (branch == null)
            {
                return NotFound();
            }

            branch.IsActive = !branch.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = branch.IsActive
                ? "Đã kích hoạt cơ sở."
                : "Đã tạm ngưng cơ sở.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var branch = await _context.tblBranch.FindAsync(id);
            if (branch == null)
            {
                return NotFound();
            }

            var hasBookings = await _context.tblBooking.AnyAsync(b => b.IdBranch == id);
            if (hasBookings)
            {
                TempData["Error"] = "Cơ sở đang có lịch đặt hoặc hoá đơn gắn liền, không thể xoá.";
                return RedirectToAction(nameof(Index));
            }

            _context.tblBranch.Remove(branch);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xoá cơ sở khỏi hệ thống.";
            return RedirectToAction(nameof(Index));
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
