using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KD_Restaurant.ViewModels
{
    public class EmployeeManagementViewModel
    {
        public List<EmployeeListItemViewModel> Employees { get; set; } = new();
        public List<SelectOption> RoleOptions { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string RoleFilter { get; set; } = "all";
        public string StatusFilter { get; set; } = "all";
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int AdminCount { get; set; }
        public int ManagerCount { get; set; }
        public int StaffCount { get; set; }
        public string? SuccessMessage { get; set; }
    }

    public class EmployeeListItemViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string RoleBadgeClass { get; set; } = "bg-secondary";
        public string? PhoneNumber { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Description { get; set; }
    }

    public class EmployeeFormModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        public int RoleId { get; set; }

        public bool IsActive { get; set; } = true;

        [DataType(DataType.Password)]
        [StringLength(64, ErrorMessage = "Mật khẩu tối đa 64 ký tự")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string? ConfirmPassword { get; set; }
    }

    public class EmployeeDetailsViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string RoleBadgeClass { get; set; } = "bg-secondary";
        public string? PhoneNumber { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
