namespace KD_Restaurant.Utilities
{
    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Staff = "Staff";

        public const string AdminOrManager = Admin + "," + Manager;
        public const string AdminManagerStaff = Admin + "," + Manager + "," + Staff;
    }
}
