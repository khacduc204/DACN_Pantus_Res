using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KD_Restaurant.ViewModels
{
    public class BookingManagementViewModel
    {
        public DateTime SelectedDate { get; set; }
        public int? SelectedBranchId { get; set; }
        public int? SelectedStatusId { get; set; }

        public List<BookingCardViewModel> Bookings { get; set; } = new();
        public List<SelectOption> Branches { get; set; } = new();
        public List<BookingStatusOption> Statuses { get; set; } = new();
        public List<SelectOption> Customers { get; set; } = new();
        public List<SelectOption> MenuItems { get; set; } = new();
        public Dictionary<int, int> StatusCounters { get; set; } = new();
    }

    public class BookingCardViewModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? TableName { get; set; }
        public int? TableId { get; set; }
        public DateTime BookingDate { get; set; }
        public string TimeSlot { get; set; } = string.Empty;
        public int? Guests { get; set; }
        public string? Note { get; set; }
        public int? StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = "bg-secondary";
        public string StatusKey { get; set; } = "pending";
        public bool HasOrder { get; set; }
        public List<BookingOrderItemViewModel> OrderItems { get; set; } = new();
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public string? CancelledByName { get; set; }
        public DateTime? LastUpdateTime { get; set; }
    }

    public class BookingOrderItemViewModel
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int? Price { get; set; }
    }

    public class BookingInProgressViewModel
    {
        public BookingCardViewModel Booking { get; set; } = new();
        public List<SelectOption> MenuItems { get; set; } = new();
        public string? ReturnUrl { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
    }

    public class BookingStatusOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BadgeClass { get; set; } = "bg-secondary";
    }

    public class BookingCreateRequest
    {
        public int? CustomerId { get; set; }

        [MaxLength(150)]
        public string? CustomerName { get; set; }

        [MaxLength(20)]
        public string? CustomerPhone { get; set; }

        public int? BranchId { get; set; }
        public int? TableId { get; set; }

        [Required]
        public DateTime BookingDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string TimeSlot { get; set; } = string.Empty;

        [Required]
        public int NumberGuests { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }

    public class BookingAssignRequest
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        public int TableId { get; set; }
    }

    public class BookingMenuRequest
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        public int MenuItemId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }

    public class BookingCancelRequest
    {
        [Required]
        public int BookingId { get; set; }

        public int? StatusId { get; set; }

        [MaxLength(255)]
        public string? Reason { get; set; }
    }

    public class BookingPaymentRequest
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int AmountGiven { get; set; }

        [MaxLength(255)]
        public string? Notes { get; set; }

        public bool PrintReceipt { get; set; }
    }

    public class WalkInCreateRequest
    {
        [MaxLength(150)]
        public string? CustomerName { get; set; }

        [MaxLength(20)]
        public string? CustomerPhone { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn chi nhánh phục vụ")]
        public int BranchId { get; set; }

        public int? TableId { get; set; }

        [Range(1, 50)]
        public int Guests { get; set; } = 2;

        [MaxLength(255)]
        public string? Note { get; set; }
    }

    public class BookingOrderUpdateRequest
    {
        [Required]
        public int DetailId { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class BookingInvoiceViewModel
    {
        public BookingCardViewModel Booking { get; set; } = new();
        public List<BookingOrderItemViewModel> OrderItems { get; set; } = new();
        public int OrderTotal { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime? PaymentTime { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
    }

    public class CurrentOrderSummaryViewModel
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? TableName { get; set; }
        public string? BranchName { get; set; }
        public DateTime BookingDate { get; set; }
        public string TimeSlot { get; set; } = string.Empty;
        public int? Guests { get; set; }
        public int TotalAmount { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = "bg-secondary";
        public string StatusKey { get; set; } = "serving";
        public bool CanCheckout { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public DateTime? PaymentTime { get; set; }
        public string? PaymentMethod { get; set; }
        public string ReferenceCode { get; set; } = string.Empty;
        public string? CashierName { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelledByName { get; set; }
    }

    public class CurrentOrderListViewModel
    {
        public List<CurrentOrderSummaryViewModel> Orders { get; set; } = new();
        public bool IsHistory { get; set; }
        public string? CustomTitle { get; set; }
        public string? CustomDescription { get; set; }
        public int? HighlightBookingId { get; set; }
        public List<SelectOption> Branches { get; set; } = new();
        public string ActiveTab { get; set; } = "all";
        public Dictionary<string, int> TabCounters { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int PageSize { get; set; } = 10;
        public string Title => CustomTitle ?? (IsHistory ? "Lịch sử hoá đơn" : "Đơn hiện tại");
        public string Description => CustomDescription ?? (IsHistory
            ? "Danh sách các hoá đơn đã hoàn tất."
            : "Theo dõi các đơn đang phục vụ và truy cập nhanh để chỉnh sửa.");
    }

    public class BookingHistoryViewModel
    {
        public List<BookingCardViewModel> Bookings { get; set; } = new();
        public string ActiveTab { get; set; } = "all";
        public Dictionary<string, int> TabCounters { get; set; } = new();
        public List<SelectOption> Branches { get; set; } = new();
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public int? SelectedBranchId { get; set; }
        public DateTime? SelectedDate { get; set; }
        public string Title { get; set; } = "Lịch sử đặt bàn";
        public string Description { get; set; } = "Theo dõi các đặt bàn đã hoàn tất hoặc huỷ bỏ.";
    }
}
