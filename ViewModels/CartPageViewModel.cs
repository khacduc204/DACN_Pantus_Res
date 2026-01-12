using KD_Restaurant.Models;
using System;
using System.Collections.Generic;

namespace KD_Restaurant.ViewModels
{
    public class CartPageViewModel
    {
        public List<CartItem> Items { get; set; } = new();
        public List<tblBranch> Branches { get; set; } = new();
        public IReadOnlyList<string> TimeSlots { get; set; } = Array.Empty<string>();
        public string? DefaultFullName { get; set; }
        public string? DefaultPhoneNumber { get; set; }
        public bool IsAuthenticated { get; set; }
    }
}
