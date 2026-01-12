using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KD_Restaurant.Models
{
    public class tblRole
    {
        [Key]
        public int IdRole { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public virtual ICollection<tblUser> Users { get; set; } = new HashSet<tblUser>();
        public virtual ICollection<tblRolePermission> Permissions { get; set; } = new HashSet<tblRolePermission>();
    }
}
