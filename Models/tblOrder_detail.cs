using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KD_Restaurant.Models
{
    public class tblOrder_detail
    {
        public int Id { get; set; }
        public int IdOrder { get; set; }
        public int? IdMenuItem { get; set; }
        public int? IdCombo { get; set; }
        public int? Quantity { get; set; }
        public int? PriceSale { get; set; }
        public int Amount { get; set; }

        [ForeignKey("IdOrder")]
        public virtual tblOrder Order { get; set; } = null!;

        [ForeignKey("IdMenuItem")]
        public virtual tblMenuItem MenuItem { get; set; } = null!;

    }
}