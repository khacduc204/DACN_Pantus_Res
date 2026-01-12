using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KD_Restaurant.Models
{
    [Table("tblTable_type")]
    public class tblTable_type
    {
        [Key]
        public int IdType { get; set; }

        [MaxLength(50)]
        public string? TypeName { get; set; }

        public int? MaxSeats { get; set; }

        public bool isActive { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        public virtual ICollection<tblTable> Tables { get; set; } = new HashSet<tblTable>();
    }
}
