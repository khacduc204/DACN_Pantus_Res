using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KD_Restaurant.Models
{
    [Table("tblRestaurant_info")]
    public class tblRestaurantInfo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string ResName { get; set; } = string.Empty;

        [MaxLength(15)]
        [Column(TypeName = "varchar(15)")]
        public string? Hotline1 { get; set; }

        [MaxLength(15)]
        [Column(TypeName = "varchar(15)")]
        public string? Hotline2 { get; set; }

        [Column("Emai", TypeName = "varchar(255)")]
        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(255)]
        [Column(TypeName = "varchar(255)")]
        public string? Logo { get; set; }

        [MaxLength(50)]
        public string? OpeningDay { get; set; }

        public TimeSpan? OpenTime { get; set; }

        public TimeSpan? CloseTime { get; set; }

        [MaxLength(255)]
        public string? SortDescription { get; set; }

        [Column(TypeName = "ntext")]
        public string? LogDescription { get; set; }
    }
}
