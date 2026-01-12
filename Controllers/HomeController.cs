using System.Diagnostics;
using KD_Restaurant.Models;
using KD_Restaurant.ViewModels;
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
            return RedirectToAction(nameof(MenuController.Index), "Menu");
        }

        public IActionResult StoreLocator()
        {
            var branches = _context.tblBranch
                .Where(b => b.IsActive)
                .OrderBy(b => b.BranchName)
                .Select(b => new StoreBranchViewModel
                {
                    Id = b.IdBranch,
                    Name = string.IsNullOrWhiteSpace(b.BranchName) ? $"Chi nhánh #{b.IdBranch}" : b.BranchName!,
                    Address = b.Address ?? "Đang cập nhật",
                    Phone = b.PhoneNumber,
                    Description = b.Description
                })
                .ToList();

            var model = new StoreLocatorViewModel
            {
                Branches = branches
            };

            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        
    }
}
