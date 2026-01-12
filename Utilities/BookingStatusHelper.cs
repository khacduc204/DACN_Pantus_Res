using System;

namespace KD_Restaurant.Utilities
{
    public static class BookingStatusHelper
    {
        public static string GetStatusName(int? statusId)
        {
            return statusId switch
            {
                1 => "Chưa nhận bàn",
                2 => "Đã nhận bàn",
                3 => "Đã huỷ",
                4 => "Đã hoàn thành",
                _ => "Không xác định"
            };
        }

        public static string GetBadgeClass(string? statusName)
        {
            if (string.IsNullOrWhiteSpace(statusName))
            {
                return "bg-secondary";
            }

            var name = statusName.ToLowerInvariant();
            if (name.Contains("chờ") || name.Contains("pending"))
            {
                return "bg-warning text-dark";
            }

            if (name.Contains("đang") || name.Contains("serve") || name.Contains("phục vụ") || name.Contains("nhận"))
            {
                return "bg-primary";
            }

            if (name.Contains("huỷ") || name.Contains("cancel"))
            {
                return "bg-dark";
            }

            if (name.Contains("xong") || name.Contains("completed") || name.Contains("hoàn"))
            {
                return "bg-success";
            }

            return "bg-info text-dark";
        }

        public static string GetStatusKey(int? statusId)
        {
            return statusId switch
            {
                3 => "cancelled",
                4 => "completed",
                2 => "serving",
                1 => "pending",
                _ => "other"
            };
        }
    }
}
