using KD_Restaurant.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KD_Restaurant.ViewComponents
{
    public class MenuItemViewComponent : ViewComponent
    {
        private readonly KDContext _context;

        public MenuItemViewComponent(KDContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var featuredItems = await _context.tblMenuItem
                .Include(m => m.Category)
                .Where(m => m.IsActive)
                .OrderByDescending(m => m.IdMenuItem)
                .ThenBy(m => m.Title)
                .Take(6)
                .ToListAsync();

            return View(featuredItems);
        }
    }
}