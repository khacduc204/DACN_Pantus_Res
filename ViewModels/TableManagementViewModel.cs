using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KD_Restaurant.ViewModels
{
    public class TableManagementViewModel
    {
        public List<TableCardViewModel> Tables { get; set; } = new();
        public List<TableStatusOption> Statuses { get; set; } = new();
        public Dictionary<int, int> StatusCounters { get; set; } = new();
        public List<BranchGroupViewModel> Branches { get; set; } = new();
        public TableCreateFormOptions CreateOptions { get; set; } = new();
        public int? SelectedBranchId { get; set; }
    }

    public class TableCardViewModel
    {
        public int Id { get; set; }
        public string TableName { get; set; } = string.Empty;
        public int? AreaId { get; set; }
        public string? AreaLabel { get; set; }
        public string? AreaDescription { get; set; }
        public int? BranchId { get; set; }
        public string? BranchLabel { get; set; }
        public int? TypeId { get; set; }
        public string? TypeName { get; set; }
        public int? MaxSeats { get; set; }
        public int? StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = "bg-secondary";
        public string? Description { get; set; }
        public bool HasActiveBooking { get; set; }
        public string? CustomerName { get; set; }
        public string? BookingTimeFrame { get; set; }
        public string? BookingNote { get; set; }
        public bool IsActive { get; set; }
    }

    public class TableStatusOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BadgeClass { get; set; } = "bg-secondary";
    }

    public class BranchGroupViewModel
    {
        public int? BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public List<AreaGroupViewModel> Areas { get; set; } = new();
    }

    public class AreaGroupViewModel
    {
        public int? AreaId { get; set; }
        public string AreaName { get; set; } = string.Empty;
        public string? AreaDescription { get; set; }
        public List<TableCardViewModel> Tables { get; set; } = new();
    }

    public class TableCreateFormOptions
    {
        public List<SelectOption> Branches { get; set; } = new();
        public List<AreaOption> Areas { get; set; } = new();
        public List<TableTypeOption> Types { get; set; } = new();
        public List<TableStatusOption> Statuses { get; set; } = new();
    }

    public class SelectOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AreaOption : SelectOption
    {
        public int? BranchId { get; set; }
    }

    public class TableTypeOption : SelectOption
    {
        public int? MaxSeats { get; set; }
        public string? Description { get; set; }
    }

    public class TableCreateRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên bàn")]
        [MaxLength(50)]
        public string TableName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn khu vực")]
        public int AreaId { get; set; }

        public int? TypeId { get; set; }

        public int? StatusId { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class TableEditRequest
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên bàn")]
        [MaxLength(50)]
        public string TableName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn khu vực")]
        public int AreaId { get; set; }

        public int? TypeId { get; set; }

        public int? StatusId { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class AreaCreateRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên khu vực")]
        [MaxLength(100)]
        public string AreaName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn cơ sở")]
        public int BranchId { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
