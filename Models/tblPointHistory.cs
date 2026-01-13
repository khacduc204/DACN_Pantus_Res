using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KD_Restaurant.Models
{
    public class tblPointHistory
    {
        [Key]
        public int IdHistory { get; set; }

        [Required]
        public int IdCard { get; set; }

        /// <summary>
        /// Earn: Tích điểm
        /// Use : Dùng điểm
        /// </summary>
        [Required]
        [StringLength(10)]
        public string ChangeType { get; set; } = string.Empty;

        /// <summary>
        /// Số điểm thay đổi (luôn là số dương)
        /// </summary>
        [Required]
        public int Points { get; set; }

        /// <summary>
        /// Tham chiếu tới đơn hàng (IdOrder)
        /// </summary>
        public int? ReferenceId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey(nameof(IdCard))]
        public virtual tblMembershipCard? MembershipCard { get; set; }
    }
}
