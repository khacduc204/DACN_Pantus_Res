using System;
using System.Linq;
using System.Threading.Tasks;
using KD_Restaurant.Models;
using KD_Restaurant.Security;
using KD_Restaurant.Utilities;
using KD_Restaurant.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KD_Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.AdminManagerStaff)]
    [PermissionAuthorize(PermissionKeys.Dashboard)]
    public class HomeController : Controller
    {
        private readonly KDContext _context;

        public HomeController(KDContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? range = null, int page = 1, int pageSize = 5, string? search = null)
        {
            var pageSizeOptions = new[] { 5, 10, 25, 50 };
            if (!pageSizeOptions.Contains(pageSize))
            {
                pageSize = pageSizeOptions.First();
            }
            page = Math.Max(1, page);
            var normalizedSearch = (search ?? string.Empty).Trim();

            var (selectedRange, label, start, end) = ResolveRange(range);

            var ordersQuery = _context.tblOrder
                .AsNoTracking()
                .Include(o => o.Booking)
                .Where(o => o.PaymentTime != null)
                .Where(o => o.Booking == null || o.Booking.IdStatus != 3);

            if (start.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= start.Value);
            }
            if (end.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate < end.Value);
            }

            var orders = await ordersQuery
                .Select(o => new
                {
                    o.IdOrder,
                    o.OrderDate,
                    o.IdCustomer,
                    Revenue = o.TotalAmount ?? 0
                })
                .ToListAsync();

            var totalOrders = orders.Count;
            var totalCustomers = orders
                .Where(o => o.IdCustomer.HasValue)
                .Select(o => o.IdCustomer!.Value)
                .Distinct()
                .Count();

            var detailQuery = _context.tblOrder_detail
                .AsNoTracking()
                .Include(d => d.MenuItem)
                    .ThenInclude(m => m.Category)
                .Include(d => d.Order)
                    .ThenInclude(o => o.Booking)
                .Where(d => d.IdMenuItem.HasValue && d.Order != null)
                .Where(d => d.Order != null && d.Order.PaymentTime != null)
                .Where(d => d.Order!.Booking == null || d.Order.Booking!.IdStatus != 3);

            if (start.HasValue)
            {
                detailQuery = detailQuery.Where(d => d.Order.OrderDate >= start.Value);
            }
            if (end.HasValue)
            {
                detailQuery = detailQuery.Where(d => d.Order.OrderDate < end.Value);
            }

            var orderMetricsRaw = await detailQuery
                .GroupBy(d => d.IdOrder)
                .Select(g => new
                {
                    OrderId = g.Key,
                    DetailRevenue = g.Sum(x => x.Amount > 0 ? x.Amount : ((x.PriceSale ?? x.MenuItem.Price) * (x.Quantity ?? 0))),
                    Cost = g.Sum(x => (x.MenuItem.PriceCost) * (x.Quantity ?? 0))
                })
                .ToListAsync();

            var orderMetrics = orderMetricsRaw
                .ToDictionary(x => x.OrderId, x => (Revenue: x.DetailRevenue, Cost: x.Cost));

            var orderSnapshots = new List<(DateTime OrderDate, int Revenue, int Cost)>();
            var totalRevenue = 0;
            var totalCost = 0;

            foreach (var order in orders)
            {
                var metrics = orderMetrics.TryGetValue(order.IdOrder, out var value)
                    ? value
                    : (Revenue: 0, Cost: 0);

                var revenue = order.Revenue > 0 ? order.Revenue : metrics.Revenue;
                totalRevenue += revenue;
                totalCost += metrics.Cost;

                orderSnapshots.Add((order.OrderDate, revenue, metrics.Cost));
            }

            var totalProfit = totalRevenue - totalCost;

            var bucketed = orderSnapshots
                .GroupBy(o => GetBucketKey(o.OrderDate, selectedRange))
                .Select(g => new
                {
                    Bucket = g.Key,
                    Revenue = g.Sum(x => x.Revenue),
                    Profit = g.Sum(x => x.Revenue - x.Cost)
                })
                .OrderBy(x => x.Bucket)
                .ToList();

            var chartPoints = bucketed
                .Select(d => new DashboardChartPoint
                {
                    Label = FormatLabel(d.Bucket, selectedRange),
                    Revenue = d.Revenue,
                    Profit = d.Profit
                })
                .ToList();

            if (!chartPoints.Any())
            {
                chartPoints.Add(new DashboardChartPoint { Label = label, Revenue = 0, Profit = 0 });
            }

            var itemStats = await detailQuery
                .GroupBy(d => new
                {
                    d.IdMenuItem,
                    Name = d.MenuItem.Title,
                    Category = d.MenuItem.Category != null ? d.MenuItem.Category.Title : null,
                    d.MenuItem.Image
                })
                .Select(g => new DashboardItemStat
                {
                    ItemId = g.Key.IdMenuItem ?? 0,
                    ItemName = string.IsNullOrWhiteSpace(g.Key.Name) ? $"Món #{g.Key.IdMenuItem}" : g.Key.Name!,
                    Category = g.Key.Category,
                    Image = g.Key.Image,
                    Quantity = g.Sum(x => x.Quantity ?? 0),
                    Revenue = g.Sum(x => x.Amount > 0 ? x.Amount : ((x.PriceSale ?? x.MenuItem.Price) * (x.Quantity ?? 0)))
                })
                .OrderByDescending(x => x.Quantity)
                .ToListAsync();

            var filteredItems = itemStats;
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                var lower = normalizedSearch.ToLowerInvariant();
                filteredItems = filteredItems
                    .Where(i =>
                        i.ItemName.ToLowerInvariant().Contains(lower) ||
                        (!string.IsNullOrWhiteSpace(i.Category) && i.Category!.ToLowerInvariant().Contains(lower)))
                    .ToList();
            }

            var totalPages = Math.Max(1, (int)Math.Ceiling(filteredItems.Count / (double)pageSize));
            page = Math.Min(page, totalPages);
            var pagedItems = filteredItems
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new AdminDashboardViewModel
            {
                Range = selectedRange,
                RangeLabel = label,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                TotalCustomers = totalCustomers,
                TotalProfit = totalProfit,
                ChartPoints = chartPoints,
                ItemStats = itemStats,
                TopItems = itemStats.Take(5).ToList(),
                Filters = BuildFilters(selectedRange),
                PagedItemStats = pagedItems,
                PageSizeOptions = pageSizeOptions.ToList(),
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                SearchTerm = string.IsNullOrWhiteSpace(normalizedSearch) ? null : normalizedSearch,
                FilteredItemCount = filteredItems.Count
            };

            return View(model);
        }

        private static (DashboardRange Range, string Label, DateTime? Start, DateTime? End) ResolveRange(string? value)
        {
            var now = DateTime.Now;
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            var range = normalized switch
            {
                "thangnay" => DashboardRange.ThisMonth,
                "namnay" => DashboardRange.ThisYear,
                "tatca" => DashboardRange.All,
                _ => DashboardRange.Today
            };

            DateTime? start = null;
            DateTime? end = null;
            string label;

            switch (range)
            {
                case DashboardRange.ThisMonth:
                    start = new DateTime(now.Year, now.Month, 1);
                    end = start.Value.AddMonths(1);
                    label = "Tháng này";
                    break;
                case DashboardRange.ThisYear:
                    start = new DateTime(now.Year, 1, 1);
                    end = start.Value.AddYears(1);
                    label = "Năm nay";
                    break;
                case DashboardRange.All:
                    label = "Tất cả";
                    break;
                default:
                    start = now.Date;
                    end = start.Value.AddDays(1);
                    label = "Hôm nay";
                    break;
            }

            return (range, label, start, end);
        }

        private static string FormatLabel(DateTime date, DashboardRange range)
        {
            return range switch
            {
                DashboardRange.Today => date.ToString("HH:mm"),
                DashboardRange.ThisMonth => date.ToString("dd/MM"),
                DashboardRange.ThisYear => date.ToString("MM/yyyy"),
                _ => date.ToString("dd/MM/yyyy")
            };
        }

        private static DateTime GetBucketKey(DateTime date, DashboardRange range)
        {
            return range switch
            {
                DashboardRange.Today => new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0),
                DashboardRange.ThisMonth => date.Date,
                DashboardRange.ThisYear => new DateTime(date.Year, date.Month, 1),
                _ => date.Date
            };
        }

        private static List<DashboardFilterOption> BuildFilters(DashboardRange active)
        {
            return new List<DashboardFilterOption>
            {
                new() { Range = DashboardRange.Today, Label = "Hôm nay", RouteValue = "homnay", IsActive = active == DashboardRange.Today },
                new() { Range = DashboardRange.ThisMonth, Label = "Tháng này", RouteValue = "thangnay", IsActive = active == DashboardRange.ThisMonth },
                new() { Range = DashboardRange.ThisYear, Label = "Năm nay", RouteValue = "namnay", IsActive = active == DashboardRange.ThisYear },
                new() { Range = DashboardRange.All, Label = "Tất cả", RouteValue = "tatca", IsActive = active == DashboardRange.All }
            };
        }
    }
}
