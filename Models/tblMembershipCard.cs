public class tblMembershipCard
{
    [Key]
    public int IdCard { get; set; }

    public int IdCustomer { get; set; }

    [StringLength(20)]
    public string CardNumber { get; set; } = string.Empty;

    public int Points { get; set; } = 0;

    [StringLength(20)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual tblCustomer? Customer { get; set; }
}
