using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KD_Restaurant.Models;
using KD_Restaurant.ViewModels;

namespace KD_Restaurant.Controllers
{
    public class MenuController : Controller
    {
        private readonly KDContext _context;

        public MenuController(KDContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int PageSize = 6;
            page = page < 1 ? 1 : page;

            var categories = await _context.tblMenuCategory
                .Where(c => c.IsActive)
                .ToListAsync();
            ViewBag.Categories = categories;

            var query = _context.tblMenuItem
                .Include(m => m.Category)
                .Where(m => m.IsActive)
                .OrderByDescending(m => m.IdMenuItem);

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));

            if (page > totalPages)
            {
                page = totalPages;
            }

            var items = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var viewModel = new MenuPageViewModel
            {
                Items = items,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = PageSize,
                TotalItems = totalItems
            };

            return View(viewModel);
        }

        [Route("/menu/{alias}-{id}.html")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menuItem = await _context.tblMenuItem
                .Include(i => i.Category)
                .FirstOrDefaultAsync(m => m.IdMenuItem == id && m.IsActive);

            if (menuItem == null)
            {
                return NotFound();
            }

            var categories = await _context.tblMenuCategory
                .Where(c => c.IsActive)
                .ToListAsync();

            var relatedItems = await _context.tblMenuItem
                .Where(i => i.IdMenuItem != id && i.IdCategory == menuItem.IdCategory && i.IsActive)
                .OrderByDescending(i => i.IdMenuItem)
                .Take(5)
                .ToListAsync();

            var reviews = await _context.tblMenuReview
                .Where(r => r.IdMenuItem == id && r.IsActive)
                .OrderByDescending(r => r.CreatedDate ?? DateTime.MinValue)
                .ToListAsync();

            var reviewModels = reviews
                .Select(r => new MenuReviewViewModel
                {
                    Id = r.IdMenuReview,
                    Name = r.Name,
                    Phone = r.Phone,
                    Detail = r.Detail,
                    Rating = r.Rating.GetValueOrDefault(),
                    CreatedDate = r.CreatedDate,
                    Image = r.Image,
                    IsActive = r.IsActive
                })
                .ToList();

            var totalReviews = reviewModels.Count;
            var averageRating = totalReviews == 0
                ? 0
                : Math.Round(reviewModels.Average(r => r.Rating), 1);

            var ratingBreakdown = Enumerable.Range(1, 5)
                .Select(star => new RatingBucketViewModel
                {
                    Star = star,
                    Count = reviewModels.Count(r => r.Rating == star),
                    Percentage = totalReviews == 0
                        ? 0
                        : Math.Round(reviewModels.Count(r => r.Rating == star) / (double)totalReviews * 100, 1)
                })
                .OrderByDescending(r => r.Star)
                .ToList();

            var viewModel = new MenuDetailViewModel
            {
                Item = menuItem,
                Categories = categories,
                RelatedItems = relatedItems,
                Reviews = reviewModels,
                AverageRating = averageRating,
                ReviewCount = totalReviews,
                RatingBreakdown = ratingBreakdown,
                SpotlightReview = reviewModels
                    .OrderByDescending(r => r.Rating)
                    .ThenByDescending(r => r.CreatedDate)
                    .FirstOrDefault()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateReview(int menuid, string name, string phone, int rating, string message)
        {
            try
            {
                if (menuid <= 0)
                {
                    return Json(new { status = false, message = "Món ăn không hợp lệ." });
                }

                var trimmedName = name?.Trim();
                var trimmedPhone = phone?.Trim();
                var trimmedMessage = message?.Trim();

                if (string.IsNullOrWhiteSpace(trimmedName) ||
                    string.IsNullOrWhiteSpace(trimmedPhone) ||
                    string.IsNullOrWhiteSpace(trimmedMessage))
                {
                    return Json(new { status = false, message = "Vui lòng điền đầy đủ thông tin đánh giá." });
                }

                var normalizedRating = Math.Max(1, Math.Min(5, rating));

                var menu = await _context.tblMenuItem.FirstOrDefaultAsync(b => b.IdMenuItem == menuid);
                if (menu == null)
                {
                    return Json(new { status = false, message = "Món ăn không tồn tại." });
                }

                var review = new tblMenuReview
                {
                    IdMenuItem = menuid,
                    Name = trimmedName,
                    Phone = trimmedPhone,
                    Rating = normalizedRating,
                    Detail = trimmedMessage,
                    CreatedDate = DateTime.Now,
                    CreatedBy = trimmedName,
                    IsActive = false,
                    Image = string.Empty
                };

                _context.Add(review);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    status = true,
                    message = "Đánh giá của bạn đã được gửi và sẽ hiển thị sau khi được quản trị viên phê duyệt."
                });
            }
            catch
            {
                return Json(new { status = false, message = "Có lỗi xảy ra, vui lòng thử lại." });
            }
        }
    }
}