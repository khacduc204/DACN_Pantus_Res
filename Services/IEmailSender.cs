using System.Threading;
using System.Threading.Tasks;

namespace KD_Restaurant.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
    }
}
