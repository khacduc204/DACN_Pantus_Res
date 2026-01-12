using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KD_Restaurant.ViewModels
{
    public class ContactFormViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(150, ErrorMessage = "Họ tên tối đa 150 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(150)]
        [Required(ErrorMessage = "Vui lòng nhập email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung liên hệ")]
        [StringLength(1000, ErrorMessage = "Nội dung tối đa 1000 ký tự")]
        public string Message { get; set; } = string.Empty;
    }

    public class ContactAdminListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class ContactAdminIndexViewModel
    {
        public IReadOnlyList<ContactAdminListItemViewModel> Contacts { get; set; } = Array.Empty<ContactAdminListItemViewModel>();
        public string? Search { get; set; }
        public string StatusFilter { get; set; } = "unread";
        public int UnreadCount { get; set; }
        public int ReadCount { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }
}
