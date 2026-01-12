using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KD_Restaurant.ViewModels
{
    public class BranchDashboardViewModel
    {
        public IReadOnlyList<BranchListItemViewModel> Branches { get; set; } = Array.Empty<BranchListItemViewModel>();
        public int TotalBranches { get; set; }
        public int ActiveBranches { get; set; }
        public int InactiveBranches => Math.Max(0, TotalBranches - ActiveBranches);
        public int TotalAreas { get; set; }
        public int UpcomingBookings { get; set; }
    }

    public class BranchListItemViewModel
    {
        public int Id { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public int AreaCount { get; set; }
        public int TableCount { get; set; }
        public int UpcomingBookings { get; set; }
        public DateTime? LastBookingDate { get; set; }
        public string? Description { get; set; }
    }

    public class BranchFormModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên cơ sở")]
        [StringLength(150)]
        [Display(Name = "Tên cơ sở")]
        public string BranchName { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [StringLength(25)]
        [Display(Name = "Điện thoại")]
        public string? PhoneNumber { get; set; }

        [StringLength(150)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả ngắn")]
        public string? Description { get; set; }

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; } = true;
    }
}
