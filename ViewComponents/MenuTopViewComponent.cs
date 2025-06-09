using KD_Restaurant.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KD_Restaurant.ViewComponents;

public class MenuTopViewComponent : ViewComponent //Lớp MenuTopViewComponent kế thừa từ ViewComponent.
{
    private readonly KDContext _context;

    public MenuTopViewComponent(KDContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var items = await _context.tblMenu
            .Where(m => m.IsActive)
            .OrderBy(m => m.Position)
            .ToListAsync();

        return View(items);
    }
}
