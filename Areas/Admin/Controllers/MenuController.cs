using KD_Restaurant.Models;
using KD_Restaurant.Security;
using KD_Restaurant.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace KD_Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.AdminManagerStaff)]
    [PermissionAuthorize(PermissionKeys.MenuStructure)]
    public class MenuController : Controller
    {
        private readonly KDContext _context;
        private readonly ILogger<MenuController> _logger;

        public MenuController(ILogger<MenuController> logger, KDContext context)
        {
            _context = context;
            _logger = logger;
        }

        // Hiển thị danh sách menu
        public IActionResult Index(string search)
        {
            var query = _context.tblMenu.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m => m.Title != null && m.Title.Contains(search));
            }
            var menus = query.OrderBy(m => m.Position).ToList();
            return View(menus);
        }

        // GET: Thêm mới menu
        public IActionResult Create()
        {
            // Nếu có menu cha, truyền ViewBag.MenuParents = _context.tblMenu.ToList();
            return View();
        }

        // POST: Thêm mới menu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(tblMenu mn)
        {
            if (ModelState.IsValid)
            {
                _context.tblMenu.Add(mn);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(mn);
        }

        // GET: Sửa menu
        public IActionResult Edit(int id)
        {
            var menu = _context.tblMenu.Find(id);
            if (menu == null)
                return NotFound();
            // Nếu có menu cha, truyền ViewBag.MenuParents = _context.tblMenu.ToList();
            return View(menu);
        }

        // POST: Sửa menu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(tblMenu mn)
        {
            if (ModelState.IsValid)
            {
                _context.tblMenu.Update(mn);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(mn);
        }

        // Xoá menu
        public IActionResult Delete(int id)
        {
            var menu = _context.tblMenu.Find(id);
            if (menu != null)
            {
                _context.tblMenu.Remove(menu);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // Đổi trạng thái hoạt động
        public IActionResult IsActive(int id)
        {
            var menu = _context.tblMenu.Find(id);
            if (menu != null)
            {
                menu.IsActive = !menu.IsActive;
                _context.tblMenu.Update(menu);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}