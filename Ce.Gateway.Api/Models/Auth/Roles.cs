namespace Ce.Gateway.Api.Models.Auth
{
    public static class Roles
    {
        public const string Administrator = "Administrator";
        public const string Management = "Management";
        public const string Monitor = "Monitor";

        public static bool IsValid(string role)
        {
            return role == Administrator || role == Management || role == Monitor;
        }
    }
}
