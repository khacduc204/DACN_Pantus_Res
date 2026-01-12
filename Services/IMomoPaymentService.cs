using System.Threading;
using System.Threading.Tasks;
using KD_Restaurant.Services.Models;

namespace KD_Restaurant.Services
{
    public interface IMomoPaymentService
    {
        Task<MomoPaymentResult> CreatePaymentAsync(MomoPaymentRequest request, CancellationToken cancellationToken = default);
    }
}
