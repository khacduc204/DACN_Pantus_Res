using System;
using System.ComponentModel.DataAnnotations;

namespace KD_Restaurant.ViewModels
{
    public class RestaurantInfoViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên nhà hàng")]
        [StringLength(255)]
        [Display(Name = "Tên nhà hàng")]
        public string ResName { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Mô tả ngắn (hiển thị dưới tiêu đề)")]
        public string? SortDescription { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập bài giới thiệu")]
        [Display(Name = "Bài giới thiệu (Tiny Cloud)")]
        public string LogDescription { get; set; } = string.Empty;

        [StringLength(15)]
        [Display(Name = "Hotline 1")]
        public string? Hotline1 { get; set; }

        [StringLength(15)]
        [Display(Name = "Hotline 2")]
        public string? Hotline2 { get; set; }

        [StringLength(255)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email liên hệ")]
        public string? Email { get; set; }

        [StringLength(255)]
        [Display(Name = "Logo hoặc ảnh giới thiệu")]
        public string? Logo { get; set; }

        [StringLength(50)]
        [Display(Name = "Ngày mở cửa (VD: Thứ 2 - Chủ nhật)")]
        public string? OpeningDay { get; set; }

        [Display(Name = "Giờ mở cửa")]
        [DataType(DataType.Time)]
        public TimeSpan? OpenTime { get; set; }

        [Display(Name = "Giờ đóng cửa")]
        [DataType(DataType.Time)]
        public TimeSpan? CloseTime { get; set; }
    }
}
