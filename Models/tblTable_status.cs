using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KD_Restaurant.Models
{
    [Table("tblTable_status")]
    public class tblTable_status
    {
        [Key]
        public int IdStatus { get; set; }

        [MaxLength(50)]
        public string? StatusName { get; set; }

        public bool isActive { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        public virtual ICollection<tblTable> Tables { get; set; } = new HashSet<tblTable>();
    }
}
