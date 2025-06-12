using System;

namespace KD_Restaurant.Models
{
    public class tblCustomer
    {
        public int IdCustomer { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }

        
        public virtual ICollection<tblBooking> tblBooking { get; set; } = new HashSet<tblBooking>();
        public virtual ICollection<tblOrder>? tblOrder { get; set; } = new HashSet<tblOrder>();
    }
}