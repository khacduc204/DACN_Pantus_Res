using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KD_Restaurant.Models
{
    public class tblBooking
    {
        public int IdBooking { get; set; }
        public int? IdCustomer { get; set; }
        public int? IdBranch { get; set; }

        public string? Email { get; set; }

        public int? IdTable { get; set; }
        public DateTime BookingDate { get; set; }
        public string? TimeSlot { get; set; }
        public int? NumberGuests { get; set; }
        public int? PrePayment { get; set; }
        public string? Note { get; set; }
        public bool isActive { get; set; }

        public int? IdStatus { get; set; }

        [ForeignKey("IdCustomer")]
        public virtual tblCustomer Customer { get; set; } = null!;

        [ForeignKey("IdBranch")]
        public virtual tblBranch Branch { get; set; } = null!;

        [ForeignKey("IdTable")]
        public virtual tblTable? Table { get; set; }

        [ForeignKey("IdStatus")]
        public virtual tblBooking_status? Status { get; set; }

        public virtual ICollection<tblOrder> tblOrder { get; set; } = new HashSet<tblOrder>();
    }
}