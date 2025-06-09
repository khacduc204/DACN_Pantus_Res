using System;

namespace KD_Restaurant.Models
{
    public partial class tblSlider
    {
        
        public int IdSlider { get; set; }
        public string ImagePath { get; set; } = null!;
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
        public int? DisplayOrder { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
