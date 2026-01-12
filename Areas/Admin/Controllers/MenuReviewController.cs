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

namespace KD_Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.AdminManagerStaff)]
    [PermissionAuthorize(PermissionKeys.MenuReviewManagement)]
    public class MenuReviewController : Controller
    {
        private readonly KDContext _context;
        private const int PageSize = 12;

        public MenuReviewController(KDContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string status = "pending", int page = 1)
        {
            var normalizedStatus = string.IsNullOrWhiteSpace(status)
                ? "pending"
                : status.Trim().ToLowerInvariant();

            var query = _context.tblMenuReview
                .Include(r => r.MenuItem)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(r =>
                    (r.Name != null && r.Name.Contains(keyword)) ||
                    (r.Phone != null && r.Phone.Contains(keyword)) ||
                    (r.Detail != null && r.Detail.Contains(keyword)) ||
                    (r.MenuItem != null && r.MenuItem.Title != null && r.MenuItem.Title.Contains(keyword)));
            }

            query = normalizedStatus switch
            {
                "published" => query.Where(r => r.IsActive),
                "all" => query,
                _ => query.Where(r => !r.IsActive)
            };

            page = Math.Max(1, page);
            var total = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            if (page > totalPages)
            {
                page = totalPages;
            }

            var reviews = await query
                .OrderBy(r => r.IsActive)
                .ThenByDescending(r => r.CreatedDate ?? DateTime.MinValue)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(r => new MenuReviewAdminListItemViewModel
                {
                    Id = r.IdMenuReview,
                    MenuTitle = r.MenuItem != null && !string.IsNullOrWhiteSpace(r.MenuItem.Title)
                        ? r.MenuItem.Title
                        : $"Món #{r.IdMenuItem}",
                    Alias = r.MenuItem != null && r.MenuItem.Alias != null ? r.MenuItem.Alias : string.Empty,
                    ReviewerName = string.IsNullOrWhiteSpace(r.Name) ? "Ẩn danh" : r.Name!,
                    Phone = r.Phone,
                    Rating = r.Rating ?? 0,
                    Detail = r.Detail,
                    IsActive = r.IsActive,
                    CreatedDate = r.CreatedDate
                })
                .ToListAsync();

            var pendingCount = await _context.tblMenuReview.CountAsync(r => !r.IsActive);
            var publishedCount = await _context.tblMenuReview.CountAsync(r => r.IsActive);
            var totalCount = pendingCount + publishedCount;

            var viewModel = new MenuReviewAdminIndexViewModel
            {
                Reviews = reviews,
                Search = search,
                StatusFilter = normalizedStatus,
                PendingCount = pendingCount,
                PublishedCount = publishedCount,
                TotalCount = totalCount,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = PageSize
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, string? status, string? search, int page = 1)
        {
            var review = await _context.tblMenuReview.FindAsync(id);
            if (review == null)
            {
                TempData["Error"] = "Không tìm thấy đánh giá.";
                return RedirectToAction(nameof(Index), new { status, search, page });
            }

            review.IsActive = !review.IsActive;
            _context.tblMenuReview.Update(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = review.IsActive
                ? "Đánh giá đã được xuất bản."
                : "Đánh giá đã chuyển sang chờ duyệt.";

            var redirectStatus = string.IsNullOrWhiteSpace(status)
                ? (review.IsActive ? "published" : "pending")
                : status;

            return RedirectToAction(nameof(Index), new { status = redirectStatus, search, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? status, string? search, int page = 1)
        {
            var review = await _context.tblMenuReview.FindAsync(id);
            if (review == null)
            {
                TempData["Error"] = "Không tìm thấy đánh giá.";
                return RedirectToAction(nameof(Index), new { status, search, page });
            }

            _context.tblMenuReview.Remove(review);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đánh giá đã được xoá.";

            return RedirectToAction(nameof(Index), new { status, search, page });
        }
    }
}
