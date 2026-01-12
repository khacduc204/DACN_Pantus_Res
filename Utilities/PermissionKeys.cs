using System.Collections.Generic;

namespace KD_Restaurant.Utilities
{
    public static class PermissionKeys
    {
        public const string Dashboard = "dashboard.view";
        public const string MenuStructure = "menu.structure";
        public const string MenuCatalog = "menu.catalog";
        public const string MenuReviewManagement = "menu.review";
        public const string ContactManagement = "contact.manage";
        public const string BookingManagement = "booking.manage";
        public const string OrderManagement = "order.manage";
        public const string TableManagement = "table.manage";
        public const string SliderManagement = "slider.manage";
        public const string CustomerManagement = "customer.manage";
        public const string EmployeeManagement = "employee.manage";
        public const string RoleManagement = "role.manage";
        public const string BranchManagement = "branch.manage";
        public const string RestaurantSettings = "restaurant.settings";

        public static readonly IReadOnlyList<string> All = new[]
        {
            Dashboard,
            MenuStructure,
            MenuCatalog,
            MenuReviewManagement,
            ContactManagement,
            BookingManagement,
            OrderManagement,
            TableManagement,
            SliderManagement,
            CustomerManagement,
            EmployeeManagement,
            RoleManagement,
            BranchManagement,
            RestaurantSettings
        };

        public static readonly IReadOnlyDictionary<string, string> DisplayNames = new Dictionary<string, string>
        {
            [Dashboard] = "Bảng điều khiển",
            [MenuStructure] = "Menu điều hướng",
            [MenuCatalog] = "Món ăn & danh mục",
            [MenuReviewManagement] = "Đánh giá món ăn",
            [ContactManagement] = "Liên hệ khách hàng",
            [BookingManagement] = "Quản lý đặt bàn",
            [OrderManagement] = "Quản lý hoá đơn",
            [TableManagement] = "Quản lý bàn",
            [SliderManagement] = "Slider & media",
            [CustomerManagement] = "Khách hàng",
            [EmployeeManagement] = "Nhân viên",
            [RoleManagement] = "Phân quyền",
            [BranchManagement] = "Cơ sở nhà hàng",
            [RestaurantSettings] = "Thông tin nhà hàng"
        };
    }
}
