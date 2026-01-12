using System;
using System.Collections.Generic;
using System.Linq;

namespace KD_Restaurant.ViewModels
{
    public enum DashboardRange
    {
        Today,
        ThisMonth,
        ThisYear,
        All
    }

    public class DashboardFilterOption
    {
        public DashboardRange Range { get; set; }
        public string Label { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string RouteValue { get; set; } = string.Empty;
    }

    public class DashboardChartPoint
    {
        public string Label { get; set; } = string.Empty;
        public int Revenue { get; set; }
        public int Profit { get; set; }
    }

    public class DashboardItemStat
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Image { get; set; }
        public int Quantity { get; set; }
        public int Revenue { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public DashboardRange Range { get; set; } = DashboardRange.Today;
        public string RangeLabel { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalRevenue { get; set; }
        public int TotalProfit { get; set; }
        public List<DashboardChartPoint> ChartPoints { get; set; } = new();
        public List<DashboardItemStat> ItemStats { get; set; } = new();
        public List<DashboardItemStat> TopItems { get; set; } = new();
        public List<DashboardFilterOption> Filters { get; set; } = new();
        public List<DashboardItemStat> PagedItemStats { get; set; } = new();
        public List<int> PageSizeOptions { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalPages { get; set; } = 1;
        public string? SearchTerm { get; set; }
        public int FilteredItemCount { get; set; }

        public string CurrencyFormat(int amount) => string.Format("{0:N0} ₫", amount);

        public DashboardChartPoint[] ChartPointsOrdered => ChartPoints
            .OrderBy(p => p.Label)
            .ToArray();
    }
}
