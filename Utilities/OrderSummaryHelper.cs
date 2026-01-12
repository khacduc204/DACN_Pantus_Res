using System;
using System.Linq;
using KD_Restaurant.Models;
using KD_Restaurant.ViewModels;
using KD_Restaurant.Utilities;

namespace KD_Restaurant.Utilities
{
    public static class OrderSummaryHelper
    {
        public static CurrentOrderSummaryViewModel FromOrder(tblOrder order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            return FromBooking(order.Booking, order);
        }

        public static CurrentOrderSummaryViewModel FromBooking(tblBooking? booking, tblOrder? order = null)
        {
            if (booking == null)
            {
                throw new ArgumentNullException(nameof(booking));
            }

            order ??= booking.tblOrder?
                .OrderByDescending(o => o.OrderDate)
                .FirstOrDefault();

            var statusName = booking.IdStatus.HasValue
                ? BookingStatusHelper.GetStatusName(booking.IdStatus)
                : booking.Status?.StatusName;

            if (string.IsNullOrWhiteSpace(statusName))
            {
                statusName = order?.PaymentTime == null ? "Đang phục vụ" : "Hoàn tất";
            }

            var statusKey = "serving";
            if (booking.IdStatus == 3 || (!booking.isActive && booking.IdStatus != 4))
            {
                statusKey = "cancelled";
            }
            else if (booking.IdStatus == 4 || order?.PaymentTime != null)
            {
                statusKey = "paid";
            }
            else if (booking.IdStatus == 1)
            {
                statusKey = "pending";
            }

            var totalAmount = order?.TotalAmount;
            var needsRecalculation = !totalAmount.HasValue || totalAmount.Value <= 0;

            if (needsRecalculation && order?.tblOrder_detail != null && order.tblOrder_detail.Any())
            {
                var recomputedTotal = 0;
                foreach (var detail in order.tblOrder_detail)
                {
                    var lineAmount = detail.Amount;
                    if (lineAmount <= 0)
                    {
                        var unitPrice = detail.PriceSale ?? detail.MenuItem?.Price ?? 0;
                        var qty = detail.Quantity.GetValueOrDefault(1);
                        lineAmount = unitPrice * qty;
                    }

                    recomputedTotal += lineAmount;
                }

                totalAmount = recomputedTotal;
                needsRecalculation = false;
            }

            if (needsRecalculation)
            {
                totalAmount = booking.PrePayment ?? 0;
            }

            var resolvedTotalAmount = totalAmount ?? 0;

            var cashierName = order?.User != null
                ? string.Join(' ', new[] { order.User.LastName, order.User.FirstName }.Where(s => !string.IsNullOrWhiteSpace(s)))
                : null;

            if (string.IsNullOrWhiteSpace(cashierName))
            {
                cashierName = order?.User?.UserName;
            }

            var latestCancellation = order?.Cancellations?
                .OrderByDescending(c => c.CancelledTime ?? DateTime.MinValue)
                .FirstOrDefault();

            var cancelledByName = latestCancellation?.CancelledByUser != null
                ? string.Join(' ', new[] { latestCancellation.CancelledByUser.LastName, latestCancellation.CancelledByUser.FirstName }
                    .Where(s => !string.IsNullOrWhiteSpace(s)))
                : null;

            if (string.IsNullOrWhiteSpace(cancelledByName))
            {
                cancelledByName = latestCancellation?.CancelledByUser?.UserName;
            }

            return new CurrentOrderSummaryViewModel
            {
                BookingId = booking.IdBooking,
                CustomerName = booking.Customer?.FullName ?? "Khách lẻ",
                CustomerPhone = booking.Customer?.PhoneNumber ?? string.Empty,
                TableName = booking.Table?.TableName,
                BranchName = booking.Branch?.BranchName,
                BookingDate = booking.BookingDate,
                TimeSlot = booking.TimeSlot ?? string.Empty,
                Guests = booking.NumberGuests,
                TotalAmount = resolvedTotalAmount,
                StatusName = statusName,
                StatusBadgeClass = BookingStatusHelper.GetBadgeClass(statusName),
                StatusKey = statusKey,
                CanCheckout = order == null || order.PaymentTime == null,
                TimeIn = order?.TimeIn,
                TimeOut = order?.TimeOut,
                PaymentTime = order?.PaymentTime,
                PaymentMethod = order?.PaymentMethod,
                ReferenceCode = $"Mã đặt bàn {booking.IdBooking}",
                CashierName = cashierName,
                CancellationReason = latestCancellation?.Description,
                CancelledAt = latestCancellation?.CancelledTime,
                CancelledByName = cancelledByName
            };
        }

    }
}
