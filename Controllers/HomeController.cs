using System.Diagnostics;
using KD_Restaurant.Models;
using Microsoft.AspNetCore.Mvc;

namespace KD_Restaurant.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly KDContext _context;

        public HomeController(KDContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var categories = _context.tblMenuCategory
                .Where(c => c.IsActive)
                .ToList();

            // Truyền xuống view qua ViewBag
            ViewBag.Categories = categories;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
         public IActionResult Menu()
        {
            var categories = _context.tblMenuCategory.Where(c => c.IsActive).ToList();
            var menuItems = _context.tblMenuItem.Where(i => i.IsActive).ToList();

            ViewBag.Categories = categories;
            return View(menuItems);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        
    }
}
