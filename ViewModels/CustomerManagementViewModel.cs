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
    }

    public class CustomerListItemViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime? LastLogin { get; set; }
        public int BookingCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class CustomerFormModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên khách hàng")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? UserName { get; set; }

        [DataType(DataType.Password)]
        [MaxLength(100)]
        public string? Password { get; set; }

        [Phone]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

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
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
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
