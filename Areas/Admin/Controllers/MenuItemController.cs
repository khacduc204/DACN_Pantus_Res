using KD_Restaurant.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KD_Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MenuItemController : Controller
    {
        private readonly KDContext _context;
        private readonly ILogger<MenuItemController> _logger;

        public MenuItemController(ILogger<MenuItemController> logger, KDContext context)
        {
            _context = context;
            _logger = logger;
        }

        // Hiển thị danh sách món ăn
        public IActionResult Index(string search)
        {
            var items = _context.tblMenuItem.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                items = items.Where(i =>
                    (i.Title != null && i.Title.Contains(search)) ||
                    (i.Alias != null && i.Alias.Contains(search))
                );
            }
            items = items.OrderBy(i => i.IdMenuItem);
            return View(items.ToList());
        }

        // GET: Thêm mới món ăn
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.tblMenuCategory.ToList(), "IdCategory", "Title");
            return View();
        }

        // POST: Thêm mới món ăn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(tblMenuItem item, IFormFile imageFile)
        {
            if (!Request.Form.ContainsKey("IsActive"))
                item.IsActive = false;
            else
                item.IsActive = true;

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Path.GetFileName(imageFile.FileName);
                    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "img", "menu");
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);
                    var filePath = Path.Combine(folderPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        imageFile.CopyTo(stream);
                    }
                    item.Image = "/assets/img/menu/" + fileName;
                }
                item.CreatedDate = DateTime.Now;
                item.CreatedBy = User.Identity?.Name ?? "admin";

                _context.tblMenuItem.Add(item);
                _context.SaveChanges();
                TempData["Success"] = "Thêm món ăn thành công!";
                return RedirectToAction("Index");
            }
            ViewBag.Categories = new SelectList(_context.tblMenuCategory.ToList(), "IdCategory", "Title", item.IdCategory);
            return View(item);
        }

        // GET: Sửa món ăn
        public IActionResult Edit(int id)
        {
            var item = _context.tblMenuItem.Find(id);
            if (item == null)
                return NotFound();
            ViewBag.Categories = new SelectList(_context.tblMenuCategory.ToList(), "IdCategory", "Title", item.IdCategory);
            return View(item);
        }

        // POST: Sửa món ăn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(tblMenuItem item, IFormFile? imageFile, string? OldImage)
        {
            ModelState.Remove("imageFile");
            ModelState.Remove("OldImage");
            if (!Request.Form.ContainsKey("IsActive"))
                item.IsActive = false;
            else
                item.IsActive = true;

            if (!ModelState.IsValid)
            {
                // In lỗi ra console để debug nếu có
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                ViewBag.Categories = new SelectList(_context.tblMenuCategory.ToList(), "IdCategory", "Title", item.IdCategory);
                return View(item);
            }

            var dbItem = _context.tblMenuItem.Find(item.IdMenuItem);
            if (dbItem == null)
                return NotFound();

            dbItem.Title = item.Title;
            dbItem.Alias = item.Alias;
            dbItem.Description = item.Description;
            dbItem.Price = item.Price;
            dbItem.PriceSale = item.PriceSale;
            dbItem.IdCategory = item.IdCategory;
            dbItem.Quantity = item.Quantity;
            dbItem.Star = item.Star;
            dbItem.Detail = item.Detail;
            dbItem.IsActive = item.IsActive;

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Path.GetFileName(imageFile.FileName);
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "img", "menu");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
                var filePath = Path.Combine(folderPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }
                dbItem.Image = "/assets/img/menu/" + fileName;
            }
            else
            {
                dbItem.Image = OldImage;
            }

            dbItem.ModifiedDate = DateTime.Now;
            dbItem.ModifiedBy = User.Identity?.Name ?? "admin";

            _context.Update(dbItem);
            _context.SaveChanges();

            TempData["Success"] = "Sửa món ăn thành công!";
            return RedirectToAction("Index");
        }

        // Xoá món ăn
        public IActionResult Delete(int id)
        {
            var item = _context.tblMenuItem.Find(id);
            if (item != null)
            {
                _context.tblMenuItem.Remove(item);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // Đổi trạng thái hoạt động
        public IActionResult IsActive(int id)
        {
            var item = _context.tblMenuItem.Find(id);
            if (item != null)
            {
                item.IsActive = !item.IsActive;
                _context.tblMenuItem.Update(item);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}