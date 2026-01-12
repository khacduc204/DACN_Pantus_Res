using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KD_Restaurant.Models
{
    [Table("tblArea")]
    public class tblArea
    {
        [Key]
        public int IdArea { get; set; }

        [MaxLength(100)]
        public string? AreaName { get; set; }

        public int? IdBranch { get; set; }

        public bool isActive { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        [ForeignKey(nameof(IdBranch))]
        public virtual tblBranch? Branch { get; set; }

        public virtual ICollection<tblTable> Tables { get; set; } = new HashSet<tblTable>();
    }
}
