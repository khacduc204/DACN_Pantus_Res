using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KD_Restaurant.Models
{
    public class tblBooking
    {
        public int IdBooking { get; set; }
        public int? IdCustomer { get; set; }
        public int? IdBranch { get; set; }

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

        public virtual ICollection<tblOrder> tblOrder { get; set; } = new HashSet<tblOrder>();
    }
}