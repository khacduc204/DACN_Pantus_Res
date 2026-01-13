using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KD_Restaurant.ViewModels
{
    public class ProfileViewModel
    {
        public int IdUser { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Họ tối đa 50 ký tự")]
        [Display(Name = "Họ")]
        public string? LastName { get; set; }

        [StringLength(50, ErrorMessage = "Tên tối đa 50 ký tự")]
        [Display(Name = "Tên")]
        public string? FirstName { get; set; }

        [Phone]
        [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [StringLength(255)]
        [Display(Name = "Mô tả ngắn")]
        public string? Description { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public IFormFile? AvatarFile { get; set; }

        public string? AvatarUrl { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        public string? ConfirmPassword { get; set; }

        public string? SuccessMessage { get; set; }
        public string? PasswordSuccess { get; set; }
        public string? PasswordError { get; set; }
        public string? MembershipMessage { get; set; }
        public bool HasMembershipCard { get; set; }
        public string? MembershipCardNumber { get; set; }
        public int MembershipPoints { get; set; }
        public string? MembershipStatus { get; set; }
        public DateTime? MembershipCreatedDate { get; set; }
        public List<PointHistoryItem> MembershipHistory { get; set; } = new();

        public class PointHistoryItem
        {
            public DateTime CreatedDate { get; set; }
            public string ChangeType { get; set; } = string.Empty;
            public int Points { get; set; }
            public int? ReferenceId { get; set; }
            public string? Description { get; set; }
        }
    }
}
