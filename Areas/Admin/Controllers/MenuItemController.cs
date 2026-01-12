using System;
using System.Linq;
using KD_Restaurant.Models;
using KD_Restaurant.Security;
using KD_Restaurant.Utilities;
using KD_Restaurant.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KD_Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.AdminManagerStaff)]
    [PermissionAuthorize(PermissionKeys.MenuCatalog)]
    public class MenuItemController : Controller
    {
        private readonly KDContext _context;
        private readonly ILogger<MenuItemController> _logger;

        public MenuItemController(ILogger<MenuItemController> logger, KDContext context)
        {
            _context = context;
            _logger = logger;
        }

        // Hiển thị dashboard quản lý thực đơn
        public IActionResult Index(string? search, int? categoryId)
        {
            var categories = _context.tblMenuCategory
                .OrderBy(c => c.Title)
                .Select(c => new MenuCategorySummaryViewModel
                {
                    Id = c.IdCategory,
                    Title = string.IsNullOrWhiteSpace(c.Title) ? $"Danh mục #{c.IdCategory}" : c.Title!,
                    Alias = c.Alias,
                    Description = c.Description,
                    Image = c.Image,
                    IsActive = c.IsActive,
                    ItemCount = c.tblMenuItems.Count
                })
                .ToList();

            var itemsQuery = _context.tblMenuItem
                .Include(i => i.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                itemsQuery = itemsQuery.Where(i =>
                    (i.Title != null && i.Title.Contains(keyword)) ||
                    (i.Description != null && i.Description.Contains(keyword)) ||
                    (i.Alias != null && i.Alias.Contains(keyword)));
            }

            if (categoryId.HasValue)
            {
                itemsQuery = itemsQuery.Where(i => i.IdCategory == categoryId.Value);
            }

            var items = itemsQuery
                .OrderByDescending(i => i.ModifiedDate ?? i.CreatedDate ?? DateTime.MinValue)
                .ThenBy(i => i.Title)
                .Select(i => new MenuItemRowViewModel
                {
                    Id = i.IdMenuItem,
                    Title = i.Title ?? $"Món #{i.IdMenuItem}",
                    Alias = i.Alias,
                    Description = i.Description,
                    CategoryName = i.Category != null && !string.IsNullOrWhiteSpace(i.Category.Title)
                        ? i.Category.Title
                        : "Chưa phân loại",
                    CategoryId = i.IdCategory,
                    Price = i.Price,
                    PriceSale = i.PriceSale,
                    Image = i.Image,
                    IsActive = i.IsActive,
                    Quantity = i.Quantity,
                    Star = i.Star
                })
                .ToList();

            var totalItems = _context.tblMenuItem.Count();
            var activeItems = _context.tblMenuItem.Count(i => i.IsActive);

            var viewModel = new MenuManagementViewModel
            {
                Categories = categories,
                Items = items,
                SearchTerm = search,
                SelectedCategoryId = categoryId,
                TotalItems = totalItems,
                ActiveItems = activeItems,
                TotalCategories = categories.Count
            };

            ViewBag.CategoryFilterOptions = new SelectList(
                categories,
                nameof(MenuCategorySummaryViewModel.Id),
                nameof(MenuCategorySummaryViewModel.Title),
                categoryId);

            return View(viewModel);
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
                item.PriceCost = Math.Max(0, item.PriceCost);
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
            dbItem.PriceCost = Math.Max(0, item.PriceCost);
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