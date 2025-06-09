namespace KD_Restaurant.Models
{
    public partial class tblMenu
    {
        public int IdMenu { get; set; }

        public string? Title { get; set; }

        public string? Alias { get; set; }

        public string? Description { get; set; }

        public int? Levels { get; set; }

        public int? ParentId { get; set; }

        public int? Position { get; set; }

        public DateTime? CreatedDate { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public string? ModifiedBy { get; set; }

        public bool IsActive { get; set; }

        public string? UrlPath { get; set; }
    }

}
