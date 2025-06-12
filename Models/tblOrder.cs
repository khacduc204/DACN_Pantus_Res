using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KD_Restaurant.Models
{
    public class tblOrder
    {
        public int IdOrder { get; set; }
        public int? IdCustomer { get; set; }
        public int? IdBooking { get; set; }
        public int? IdTable { get; set; }
        public int? IdUser { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public int? TotalCost { get; set; }
        public int? TotalAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime? PaymentTime { get; set; }
        public int? Status { get; set; }
        public string? Notes { get; set; }

        [ForeignKey("IdBooking")]
        public virtual tblBooking Booking { get; set; } = null!;

        [ForeignKey("IdCustomer")]
        public virtual tblCustomer Customer { get; set; } = null!;

        [InverseProperty("Order")]
        public virtual ICollection<tblOrder_detail> tblOrder_detail { get; set; } = new HashSet<tblOrder_detail>();
    }
}