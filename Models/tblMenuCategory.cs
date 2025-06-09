using System;

namespace KD_Restaurant.Models
{
    public partial class tblMenuCategory
    {
        public int IdCategory { get; set; }

        public string? Title { get; set; }

        public string? Alias { get; set; }

        public string? Description { get; set; }

        public string? Image { get; set; }

        public bool IsActive { get; set; }

        // Navigation property
        public virtual ICollection<tblMenuItem> tblMenuItems { get; set; } = new List<tblMenuItem>();
    }
}