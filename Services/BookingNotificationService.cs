using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KD_Restaurant.Models;
using Microsoft.Extensions.Logging;

namespace KD_Restaurant.Services
{
    public class BookingNotificationService : IBookingNotificationService
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<BookingNotificationService> _logger;

        public BookingNotificationService(IEmailSender emailSender, ILogger<BookingNotificationService> logger)
        {
            _emailSender = emailSender;
            _logger = logger;
        }

        public Task SendTableAssignmentEmailAsync(tblBooking booking, tblTable table, CancellationToken cancellationToken = default)
        {
            if (!HasEmail(booking))
            {
                return Task.CompletedTask;
            }

            var subject = "KD Restaurant - Đặt bàn của bạn đã được xếp bàn";
            var body = BuildAssignmentBody(booking, table);
            return SendSafeAsync(booking.Email!, subject, body, cancellationToken);
        }

        public Task SendBookingCancelledEmailAsync(tblBooking booking, string? reason, CancellationToken cancellationToken = default)
        {
            if (!HasEmail(booking))
            {
                return Task.CompletedTask;
            }

            var subject = "KD Restaurant - Cập nhật trạng thái đặt bàn";
            var body = BuildCancellationBody(booking, reason);
            return SendSafeAsync(booking.Email!, subject, body, cancellationToken);
        }

        private async Task SendSafeAsync(string email, string subject, string body, CancellationToken cancellationToken)
        {
            try
            {
                await _emailSender.SendEmailAsync(email, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể gửi email thông báo tới {Email}", email);
            }
        }

        private static bool HasEmail(tblBooking booking) => !string.IsNullOrWhiteSpace(booking.Email);

        private static string BuildAssignmentBody(tblBooking booking, tblTable? table)
        {
            var customerName = !string.IsNullOrWhiteSpace(booking.Customer?.FullName)
                ? booking.Customer!.FullName!
                : "quý khách";
            var branchName = booking.Branch?.BranchName
                ?? table?.Area?.Branch?.BranchName
                ?? "KD Restaurant";
            var branchAddress = booking.Branch?.Address
                ?? table?.Area?.Branch?.Address
                ?? "Hệ thống chi nhánh KD Restaurant";
            var tableName = table?.TableName ?? "bàn phù hợp";
            var slot = string.IsNullOrWhiteSpace(booking.TimeSlot) ? "chưa cập nhật" : booking.TimeSlot;
            var builder = new StringBuilder();

            builder.AppendLine($"<p>Chào {customerName},</p>");
            builder.AppendLine("<p>Đơn đặt bàn của bạn đã được xếp bàn thành công.</p>");
            builder.AppendLine("<ul>");
            builder.AppendLine($"<li>Chi nhánh: <strong>{branchName}</strong></li>");
            builder.AppendLine($"<li>Địa chỉ: {branchAddress}</li>");
            builder.AppendLine($"<li>Thời gian: {booking.BookingDate:dd/MM/yyyy} - {slot}</li>");
            builder.AppendLine($"<li>Bàn phục vụ: {tableName}</li>");
            builder.AppendLine($"<li>Số khách dự kiến: {booking.NumberGuests ?? 0}</li>");
            builder.AppendLine("</ul>");
            builder.AppendLine("<p>Vui lòng đến đúng giờ để chúng tôi phục vụ tốt nhất. Nếu cần hỗ trợ, hãy phản hồi email này hoặc liên hệ hotline của chi nhánh.</p>");
            builder.AppendLine("<p>Trân trọng,<br/>KD Restaurant</p>");

            return builder.ToString();
        }

        private static string BuildCancellationBody(tblBooking booking, string? reason)
        {
            var customerName = !string.IsNullOrWhiteSpace(booking.Customer?.FullName)
                ? booking.Customer!.FullName!
                : "quý khách";
            var branchName = booking.Branch?.BranchName ?? "KD Restaurant";
            var builder = new StringBuilder();

            builder.AppendLine($"<p>Chào {customerName},</p>");
            builder.AppendLine("<p>Rất tiếc phải thông báo rằng đặt bàn của bạn đã được huỷ.");
            builder.AppendLine("<ul>");
            builder.AppendLine($"<li>Chi nhánh: <strong>{branchName}</strong></li>");
            builder.AppendLine($"<li>Ngày/giờ: {booking.BookingDate:dd/MM/yyyy} - {(string.IsNullOrWhiteSpace(booking.TimeSlot) ? "chưa cập nhật" : booking.TimeSlot)}</li>");
            builder.AppendLine("</ul>");

            if (!string.IsNullOrWhiteSpace(reason))
            {
                builder.AppendLine($"<p>Lý do: {reason}</p>");
            }

            builder.AppendLine("<p>Nếu bạn cần đặt lại hoặc có câu hỏi, xin vui lòng liên hệ đội ngũ KD Restaurant. Chúng tôi xin lỗi vì sự bất tiện này.</p>");
            builder.AppendLine("<p>Trân trọng,<br/>KD Restaurant</p>");

            return builder.ToString();
        }
    }
}
