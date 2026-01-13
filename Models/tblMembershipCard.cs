using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KD_Restaurant.Models
{
    public class tblMembershipCard
    {
        [Key]
        public int IdCard { get; set; }

        [Required]
        public int IdCustomer { get; set; }

        [Required]
        [StringLength(20)]
        public string CardNumber { get; set; } = string.Empty;

        public int Points { get; set; } = 0;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey(nameof(IdCustomer))]
        public virtual tblCustomer? Customer { get; set; }

        public virtual ICollection<tblPointHistory> PointHistories { get; set; } = new HashSet<tblPointHistory>();
    }
}
