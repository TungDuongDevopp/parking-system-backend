namespace ParkingSystem.Authorization.Roles;

public static class StaticRoleNames
{
    public static class Host
    {
        public const string Admin = "Admin";
        public const string Staff = "Staff";
    }

    public static class Tenants
    {
        public const string Admin = "Admin";
        public const string Staff = "Staff";
        public const string Customer = "Customer";
    }
}
