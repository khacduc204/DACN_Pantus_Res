using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KD_Restaurant.Models
{
    [Table("tblTables")]
    public class tblTable
    {
        [Key]
        public int IdTable { get; set; }

        [MaxLength(50)]
        public string? TableName { get; set; }

        public int? IdArea { get; set; }

        public int? IdType { get; set; }

        public int? IdStatus { get; set; }

        public bool isActive { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        [ForeignKey(nameof(IdArea))]
        public virtual tblArea? Area { get; set; }

        [ForeignKey(nameof(IdType))]
        public virtual tblTable_type? Type { get; set; }

        [ForeignKey(nameof(IdStatus))]
        public virtual tblTable_status? Status { get; set; }

        public virtual ICollection<tblBooking> Bookings { get; set; } = new HashSet<tblBooking>();
    }
}
