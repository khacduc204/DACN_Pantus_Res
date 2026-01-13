using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace KD_Restaurant.Models
{
    public class tblCustomer
    {
        public int IdCustomer { get; set; }
        public int? IdUser { get; set; }
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }

        [ForeignKey(nameof(IdUser))]
        public virtual tblUser? User { get; set; }

        [InverseProperty(nameof(tblMembershipCard.Customer))]
        public virtual tblMembershipCard? MembershipCard { get; set; }

        public virtual ICollection<tblBooking> tblBooking { get; set; } = new HashSet<tblBooking>();
        public virtual ICollection<tblOrder> tblOrder { get; set; } = new HashSet<tblOrder>();
    }
}