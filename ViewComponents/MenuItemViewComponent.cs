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
            var categories = await _context.tblMenuCategory
                .Where(c => c.IsActive)
                .Include(c => c.tblMenuItems.Where(m => m.IsActive))
                .ToListAsync();

            return View(categories); // Trả về List<tblMenuCategory>
        }
    }
}