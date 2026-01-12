using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KD_Restaurant.Models
{
    public class tblOrder_cancelled
    {
        public int Id { get; set; }

        public int? IdOrder { get; set; }

        public int? CancelledBy { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Column("CancellDate")]
        public DateTime? CancelledTime { get; set; }

        [ForeignKey(nameof(IdOrder))]
        public virtual tblOrder? Order { get; set; }

        [ForeignKey(nameof(CancelledBy))]
        public virtual tblUser? CancelledByUser { get; set; }
    }
}
