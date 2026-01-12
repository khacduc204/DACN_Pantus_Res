using System;
using System.ComponentModel.DataAnnotations;

namespace KD_Restaurant.Models
{
    public class tblUser
    {
        [Key]
        public int IdUser { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(512)]
        public string Password { get; set; } = string.Empty;

        [StringLength(50)]
        public string? LastName { get; set; }

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(255)]
        public string? Avatar { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public int IdRole { get; set; }

        public DateTime? LastLogin { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(255)]
        public string? Description { get; set; }

        public virtual tblRole? Role { get; set; }
    }
}
