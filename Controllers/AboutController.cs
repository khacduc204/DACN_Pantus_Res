using System.Threading.Tasks;
using KD_Restaurant.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KD_Restaurant.Controllers
{
    public class AboutController : Controller
    {
        private readonly KDContext _context;

        public AboutController(KDContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var info = await _context.tblRestaurantInfo
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return View(info);
        }
    }
}
