using System;
using System.ComponentModel.DataAnnotations.Schema; // Thêm dòng này

namespace KD_Restaurant.Models
{
    public partial class tblMenuItem
    {
        public int IdMenuItem { get; set; }
        public string? Title { get; set; }
        public string? Alias { get; set; }
        public string? Description { get; set; }
        public int Price { get; set; }
        public string? Image { get; set; }
        public int IdCategory { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public int? PriceSale { get; set; }
        public int Quantity { get; set; }
        public int Star { get; set; }
        public string? Detail { get; set; }

        // Navigation Property
        public virtual tblMenuCategory? Category { get; set; }
        public virtual ICollection<tblMenuReview> tblMenuReview { get; set; } = new HashSet<tblMenuReview>();
        
        [InverseProperty("MenuItem")]
        public virtual ICollection<tblOrder_detail> tblOrder_details { get; set; } = new HashSet<tblOrder_detail>();
    }
}
