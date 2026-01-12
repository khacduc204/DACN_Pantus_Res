using System;
using System.Collections.Generic;
using System.Linq;
using KD_Restaurant.Models;

namespace KD_Restaurant.ViewModels
{
    public class MenuDetailViewModel
    {
        public tblMenuItem Item { get; set; } = null!;
        public IReadOnlyList<tblMenuItem> RelatedItems { get; set; } = Array.Empty<tblMenuItem>();
        public IReadOnlyList<tblMenuCategory> Categories { get; set; } = Array.Empty<tblMenuCategory>();
        public IReadOnlyList<MenuReviewViewModel> Reviews { get; set; } = Array.Empty<MenuReviewViewModel>();
        public IReadOnlyList<RatingBucketViewModel> RatingBreakdown { get; set; } = Array.Empty<RatingBucketViewModel>();
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public MenuReviewViewModel? SpotlightReview { get; set; }
    }

    public class MenuReviewViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Ẩn danh" : Name!;
        public string? Phone { get; set; }
        public string? Detail { get; set; }
        public int Rating { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? Image { get; set; }
        public bool IsActive { get; set; }

        public string Initials
        {
            get
            {
                var resolved = DisplayName?.Trim();
                if (string.IsNullOrEmpty(resolved))
                {
                    return "?";
                }

                var parts = resolved
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(2)
                    .Select(p => char.ToUpperInvariant(p[0]))
                    .ToArray();

                if (parts.Length == 0)
                {
                    return resolved.Substring(0, 1).ToUpperInvariant();
                }

                return new string(parts);
            }
        }
    }

    public class RatingBucketViewModel
    {
        public int Star { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class MenuReviewAdminListItemViewModel
    {
        public int Id { get; set; }
        public string MenuTitle { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public string ReviewerName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public int Rating { get; set; }
        public string? Detail { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class MenuReviewAdminIndexViewModel
    {
        public IReadOnlyList<MenuReviewAdminListItemViewModel> Reviews { get; set; } = Array.Empty<MenuReviewAdminListItemViewModel>();
        public string? Search { get; set; }
        public string StatusFilter { get; set; } = "pending";
        public int PendingCount { get; set; }
        public int PublishedCount { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }
}
