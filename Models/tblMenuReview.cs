using System;
using System.Collections.Generic;

namespace KD_Restaurant.Models;

public partial class tblMenuReview
{
    public int IdMenuReview { get; set; }

    public int IdMenuItem { get; set; }

    public string? Name { get; set; }

    public string? Phone { get; set; }

    public int? Rating { get; set; }

    public string? Detail { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public string? Image { get; set; }

    public bool IsActive { get; set; }

    public virtual tblMenuItem MenuItem { get; set; } = null!;
}
