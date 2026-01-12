using System.ComponentModel.DataAnnotations;

namespace KD_Restaurant.Models
{
    public class tblRolePermission
    {
        [Required]
        public int IdRole { get; set; }

        [Required]
        [StringLength(100)]
        public string PermissionKey { get; set; } = string.Empty;

        public bool IsAllowed { get; set; }

        public virtual tblRole? Role { get; set; }
    }
}
