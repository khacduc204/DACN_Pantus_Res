namespace KD_Restaurant.Models
{
    public class tblBranch
    {
        public int IdBranch { get; set; }
        public string? BranchName { get; set; }

        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<tblBooking> tblBooking { get; set; } = new HashSet<tblBooking>();
    }
}