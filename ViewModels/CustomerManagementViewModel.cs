using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KD_Restaurant.ViewModels
{
    public class CustomerManagementViewModel
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int NewCustomersThisWeek { get; set; }
        public string StatusFilter { get; set; } = "all";
        public string? SearchTerm { get; set; }
        public List<CustomerListItemViewModel> Customers { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalFilteredCustomers { get; set; }
        public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling((double)Math.Max(0, TotalFilteredCustomers) / PageSize);
    }

    public class CustomerListItemViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateTime? LastLogin { get; set; }
        public int BookingCount { get; set; }
        public bool IsActive { get; set; }
        public string? AccountUserName { get; set; }
        public bool HasAccount => !string.IsNullOrWhiteSpace(AccountUserName);
    }

    public class CustomerFormModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên khách hàng")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [EmailAddress]
        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(255)]
        public string? Avatar { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class CustomerDetailsViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? AccountUserName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Avatar { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }
        public int BookingCount { get; set; }
        public int OrderCount { get; set; }
        public List<CustomerTimelineItem> RecentBookings { get; set; } = new();
    }

    public class CustomerTimelineItem
    {
        public int BookingId { get; set; }
        public DateTime BookingDate { get; set; }
        public string? TimeSlot { get; set; }
        public string? Status { get; set; }
        public int? NumberGuests { get; set; }
    }
}
