using System.Collections.Generic;

namespace KD_Restaurant.ViewModels
{
    public class StoreLocatorViewModel
    {
        public string PageTitle { get; set; } = "Tìm nhà hàng gần bạn";
        public string IntroText { get; set; } = "Bật định vị hoặc nhập địa chỉ để xem chi nhánh KD Restaurant thuận tiện nhất.";
        public double DefaultLatitude { get; set; } = 18.6796; // Vinh, Nghe An
        public double DefaultLongitude { get; set; } = 105.6813;
        public string DefaultCityName { get; set; } = "Vinh, Nghệ An";
        public IList<StoreBranchViewModel> Branches { get; set; } = new List<StoreBranchViewModel>();
    }

    public class StoreBranchViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Description { get; set; }
    }
}
