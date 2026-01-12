using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KD_Restaurant.ViewModels
{
    public class MenuManagementViewModel
    {
        public List<MenuCategorySummaryViewModel> Categories { get; set; } = new();
        public List<MenuItemRowViewModel> Items { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int? SelectedCategoryId { get; set; }
        public int TotalItems { get; set; }
        public int ActiveItems { get; set; }
        public int TotalCategories { get; set; }
    }

    public class MenuCategorySummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Alias { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }
        public bool IsActive { get; set; }
        public int ItemCount { get; set; }
    }

    public class MenuItemRowViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Alias { get; set; }
        public string? Description { get; set; }
        public string? CategoryName { get; set; }
        public int? CategoryId { get; set; }
        public int Price { get; set; }
        public int? PriceSale { get; set; }
        public string? Image { get; set; }
        public bool IsActive { get; set; }
        public int Quantity { get; set; }
        public int Star { get; set; }
    }

    public class MenuCategoryFormModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Alias { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(255)]
        public string? ImageUrl { get; set; }
    }

    public class MenuCategoryManagementViewModel
    {
        public List<MenuCategorySummaryViewModel> Categories { get; set; } = new();
        public MenuCategoryFormModel CreateModel { get; set; } = new();
        public MenuCategoryFormModel? EditModel { get; set; }
        public int TotalItems { get; set; }
        public int ActiveItems { get; set; }
        public int InactiveItems => TotalItems - ActiveItems;
        public int? SelectedCategoryId { get; set; }
    }
}
