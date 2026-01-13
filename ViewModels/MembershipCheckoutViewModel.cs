using System;

namespace KD_Restaurant.ViewModels
{
    public class MembershipCheckoutViewModel
    {
        public bool IsMember { get; set; }
        public bool CanRedeem { get; set; }
        public string? CardNumber { get; set; }
        public string? Status { get; set; }
        public int AvailablePoints { get; set; }
        public int EarnUnit { get; set; }
        public int RedeemUnit { get; set; }
        public int MaxRedeemPoints { get; set; }
        public int CurrentRedeemPoints { get; set; }
        public int RedeemAmount { get; set; }
        public int PayableAmount { get; set; }
        public string? Message { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
