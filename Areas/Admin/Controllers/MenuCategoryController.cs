using System;
using System.Linq;
using KD_Restaurant.Models;
using KD_Restaurant.Security;
using KD_Restaurant.Utilities;
using KD_Restaurant.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KD_Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.AdminManagerStaff)]
    [PermissionAuthorize(PermissionKeys.MenuCatalog)]
    public class MenuCategoryController : Controller
    {
        private readonly KDContext _context;

        public MenuCategoryController(KDContext context)
        {
            _context = context;
        }

        public IActionResult Index(int? id)
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

            MenuCategoryFormModel? editModel = null;
            if (id.HasValue)
            {
                var target = _context.tblMenuCategory.FirstOrDefault(c => c.IdCategory == id.Value);
                if (target != null)
                {
                    editModel = new MenuCategoryFormModel
                    {
                        Id = target.IdCategory,
                        Title = target.Title ?? string.Empty,
                        Alias = target.Alias,
                        Description = target.Description,
                        ImageUrl = target.Image,
                        IsActive = target.IsActive
                    };
                }
            }

            var viewModel = new MenuCategoryManagementViewModel
            {
                Categories = categories,
                CreateModel = new MenuCategoryFormModel(),
                EditModel = editModel,
                TotalItems = categories.Count,
                ActiveItems = categories.Count(c => c.IsActive),
                SelectedCategoryId = id
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MenuCategoryFormModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }

            var category = new tblMenuCategory
            {
                Title = model.Title.Trim(),
                Alias = Normalize(model.Alias),
                Description = Normalize(model.Description),
                Image = Normalize(model.ImageUrl),
                IsActive = model.IsActive
            };

            _context.tblMenuCategory.Add(category);
            _context.SaveChanges();

            TempData["Success"] = $"Đã thêm danh mục \"{category.Title}\".";
            return RedirectToAction(nameof(Index), new { id = category.IdCategory });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(MenuCategoryFormModel model)
        {
            if (!model.Id.HasValue)
            {
                TempData["Error"] = "Không tìm thấy danh mục cần cập nhật.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index), new { id = model.Id });
            }

            var category = _context.tblMenuCategory.FirstOrDefault(c => c.IdCategory == model.Id.Value);
            if (category == null)
            {
                TempData["Error"] = "Danh mục không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            category.Title = model.Title.Trim();
            category.Alias = Normalize(model.Alias);
            category.Description = Normalize(model.Description);
            category.Image = Normalize(model.ImageUrl);
            category.IsActive = model.IsActive;

            _context.SaveChanges();

            TempData["Success"] = $"Đã cập nhật danh mục \"{category.Title}\".";
            return RedirectToAction(nameof(Index), new { id = category.IdCategory });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var category = _context.tblMenuCategory
                .Include(c => c.tblMenuItems)
                .FirstOrDefault(c => c.IdCategory == id);

            if (category == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục cần xoá.";
                return RedirectToAction(nameof(Index));
            }

            if (category.tblMenuItems.Any())
            {
                TempData["Error"] = "Không thể xoá danh mục đang được sử dụng cho món ăn.";
                return RedirectToAction(nameof(Index), new { id });
            }

            _context.tblMenuCategory.Remove(category);
            _context.SaveChanges();

            TempData["Success"] = $"Đã xoá danh mục \"{category.Title ?? category.IdCategory.ToString()}\".";
            return RedirectToAction(nameof(Index));
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
