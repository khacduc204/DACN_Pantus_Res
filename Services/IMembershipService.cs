using KD_Restaurant.Models;

namespace KD_Restaurant.Services
{
    public interface IMembershipService
    {
        int EarnUnitVnd { get; }
        int RedeemUnitVnd { get; }
        Task<tblMembershipCard?> GetCardByCustomerIdAsync(int customerId, bool includeHistory = false, CancellationToken cancellationToken = default);
        Task<tblMembershipCard> EnrollCustomerAsync(tblCustomer customer, CancellationToken cancellationToken = default);
        Task<(int redeemedPoints, int redeemAmount, string? error)> RedeemPointsAsync(tblCustomer customer, int requestedPoints, int orderId, CancellationToken cancellationToken = default);
        Task<int> AwardPointsAsync(tblCustomer customer, int orderId, int baseAmount, CancellationToken cancellationToken = default);
        int CalculateEarnablePoints(int totalAmount);
        int ConvertPointsToAmount(int points);
        int ConvertAmountToPoints(int amount);
    }
}
