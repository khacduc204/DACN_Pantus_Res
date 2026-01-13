using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using KD_Restaurant.Models;
using Microsoft.EntityFrameworkCore;

namespace KD_Restaurant.Services
{
    public class MembershipService : IMembershipService
    {
        private const int EarnRateVndPerPoint = 10000; // 1 điểm cho mỗi 10.000đ
        private const int RedeemValuePerPoint = 1000;   // 1 điểm quy đổi 1.000đ
        private readonly KDContext _context;

        public MembershipService(KDContext context)
        {
            _context = context;
        }

        public int EarnUnitVnd => EarnRateVndPerPoint;
        public int RedeemUnitVnd => RedeemValuePerPoint;

        public int CalculateEarnablePoints(int totalAmount) => Math.Max(totalAmount, 0) / EarnRateVndPerPoint;

        public int ConvertPointsToAmount(int points) => Math.Max(points, 0) * RedeemValuePerPoint;

        public int ConvertAmountToPoints(int amount) => Math.Max(amount, 0) / RedeemValuePerPoint;

        public async Task<tblMembershipCard?> GetCardByCustomerIdAsync(int customerId, bool includeHistory = false, CancellationToken cancellationToken = default)
        {
            IQueryable<tblMembershipCard> query = _context.tblMembershipCard;
            if (includeHistory)
            {
                query = query.Include(c => c.PointHistories);
            }

            var card = await query.FirstOrDefaultAsync(c => c.IdCustomer == customerId, cancellationToken);
            if (card != null && includeHistory && card.PointHistories != null)
            {
                card.PointHistories = card.PointHistories
                    .OrderByDescending(h => h.CreatedDate)
                    .ToList();
            }

            return card;
        }

        public async Task<tblMembershipCard> EnrollCustomerAsync(tblCustomer customer, CancellationToken cancellationToken = default)
        {
            if (customer.MembershipCard != null)
            {
                return customer.MembershipCard;
            }

            var existing = await _context.tblMembershipCard.FirstOrDefaultAsync(c => c.IdCustomer == customer.IdCustomer, cancellationToken);
            if (existing != null)
            {
                customer.MembershipCard = existing;
                return existing;
            }

            var card = new tblMembershipCard
            {
                IdCustomer = customer.IdCustomer,
                CardNumber = await GenerateCardNumberAsync(cancellationToken),
                Points = 0,
                Status = "Active",
                CreatedDate = DateTime.Now
            };

            _context.tblMembershipCard.Add(card);
            await _context.SaveChangesAsync(cancellationToken);
            customer.MembershipCard = card;
            return card;
        }

        public async Task<(int redeemedPoints, int redeemAmount, string? error)> RedeemPointsAsync(tblCustomer customer, int requestedPoints, int orderId, CancellationToken cancellationToken = default)
        {
            if (customer == null)
            {
                return (0, 0, "Không tìm thấy khách hàng để dùng điểm");
            }

            var card = customer.MembershipCard ?? await GetCardByCustomerIdAsync(customer.IdCustomer, includeHistory: false, cancellationToken: cancellationToken);
            if (card == null)
            {
                return (0, 0, "Bạn chưa đăng ký thẻ thành viên.");
            }

            if (!string.Equals(card.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return (0, 0, "Thẻ thành viên đang tạm khóa, không thể sử dụng điểm.");
            }

            var alreadyRedeemed = await _context.tblPointHistory
                .AnyAsync(h => h.IdCard == card.IdCard && h.ReferenceId == orderId && h.ChangeType == PointHistoryTypes.Use, cancellationToken);
            if (alreadyRedeemed)
            {
                return (0, 0, "Đơn này đã sử dụng điểm trước đó.");
            }

            var sanitized = Math.Min(Math.Max(requestedPoints, 0), card.Points);
            if (sanitized <= 0)
            {
                return (0, 0, "Số điểm yêu cầu không hợp lệ.");
            }

            var redeemAmount = ConvertPointsToAmount(sanitized);
            if (redeemAmount <= 0)
            {
                return (0, 0, "Không thể quy đổi số điểm này.");
            }

            card.Points -= sanitized;
            _context.tblPointHistory.Add(new tblPointHistory
            {
                IdCard = card.IdCard,
                ChangeType = PointHistoryTypes.Use,
                Points = sanitized,
                ReferenceId = orderId,
                CreatedDate = DateTime.Now
            });

            await _context.SaveChangesAsync(cancellationToken);
            return (sanitized, redeemAmount, null);
        }

        public async Task<int> AwardPointsAsync(tblCustomer customer, int orderId, int baseAmount, CancellationToken cancellationToken = default)
        {
            if (customer == null || baseAmount <= 0)
            {
                return 0;
            }

            var card = customer.MembershipCard ?? await GetCardByCustomerIdAsync(customer.IdCustomer, includeHistory: false, cancellationToken: cancellationToken);
            if (card == null || !string.Equals(card.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var alreadyEarned = await _context.tblPointHistory
                .AnyAsync(h => h.IdCard == card.IdCard && h.ReferenceId == orderId && h.ChangeType == PointHistoryTypes.Earn, cancellationToken);
            if (alreadyEarned)
            {
                return 0;
            }

            var points = CalculateEarnablePoints(baseAmount);
            if (points <= 0)
            {
                return 0;
            }

            card.Points += points;
            _context.tblPointHistory.Add(new tblPointHistory
            {
                IdCard = card.IdCard,
                ChangeType = PointHistoryTypes.Earn,
                Points = points,
                ReferenceId = orderId,
                CreatedDate = DateTime.Now
            });

            await _context.SaveChangesAsync(cancellationToken);
            return points;
        }

        private async Task<string> GenerateCardNumberAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                var random = RandomNumberGenerator.GetInt32(0, 1_000_000);
                var candidate = $"KD{DateTime.UtcNow:yy}{random:000000}";
                var exists = await _context.tblMembershipCard.AnyAsync(c => c.CardNumber == candidate, cancellationToken);
                if (!exists)
                {
                    return candidate;
                }
            }
        }

        private static class PointHistoryTypes
        {
            public const string Earn = "Earn";
            public const string Use = "Use";
        }
    }
}
