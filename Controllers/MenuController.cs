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

            ViewBag.MenuReviews = _context.tblMenuReview
                .Where(r => r.IdMenuItem == id && r.IsActive)
                .ToList();

            ViewBag.MenuRelated = _context.tblMenuItem
                .Where(i => i.IdMenuItem != id && i.IdCategory == menuItem.IdCategory && i.IsActive)
                .OrderByDescending(i => i.IdMenuItem)
                .Take(5)
                .ToList();

            ViewBag.MenuCategories = _context.tblMenuCategory
                .Where(c => c.IsActive)
                .ToList();

            return View(menuItem);
        }

        [HttpPost]
        public async Task<IActionResult> CreateReview(int menuid, string name, string phone, int rating, string message)
        {
            try
            {
                var menu = await _context.tblMenuItem.FirstOrDefaultAsync(b => b.IdMenuItem == menuid);
                if (menu == null)
                {
                    return Json(new { status = false, message = "Món ăn không tồn tại." });
                }

                var review = new tblMenuReview
                {
                    IdMenuItem = menuid,
                    Name = name,
                    Phone = phone,
                    Rating = rating,
                    Detail = message,
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                    Image = ""
                };

                _context.Add(review);
                await _context.SaveChangesAsync();

                return Json(new { status = true });
            }
            catch
            {
                return Json(new { status = false });
            }
        }
    }
}