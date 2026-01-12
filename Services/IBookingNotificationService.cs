using System.Threading;
using System.Threading.Tasks;
using KD_Restaurant.Models;

namespace KD_Restaurant.Services
{
    public interface IBookingNotificationService
    {
        Task SendTableAssignmentEmailAsync(tblBooking booking, tblTable table, CancellationToken cancellationToken = default);
        Task SendBookingCancelledEmailAsync(tblBooking booking, string? reason, CancellationToken cancellationToken = default);
    }
}
