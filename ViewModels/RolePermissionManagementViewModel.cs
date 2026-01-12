using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KD_Restaurant.ViewModels
{
    public class RolePermissionManagementViewModel
    {
        public List<RolePermissionCardViewModel> Roles { get; set; } = new();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class RolePermissionCardViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Dictionary<string, bool> Permissions { get; set; } = new();
    }

    public class RolePermissionFormModel
    {
        public int RoleId { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public List<string> GrantedPermissions { get; set; } = new();
        public bool IsLockedName { get; set; }
    }
}
