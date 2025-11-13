namespace Ce.Gateway.Api.Models.Auth
{
    /// <summary>
    /// Defines all available roles in the system
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// Administrator role - Full system access
        /// </summary>
        public const string Administrator = "Administrator";
        
        /// <summary>
        /// Management role - View users and manage operations
        /// </summary>
        public const string Management = "Management";
        
        /// <summary>
        /// Monitor role - View-only access to dashboards
        /// </summary>
        public const string Monitor = "Monitor";
        
        /// <summary>
        /// Helper property to get all roles as an array
        /// </summary>
        public static readonly string[] All = new[] { Administrator, Management, Monitor };
        
        /// <summary>
        /// Roles that can view user management
        /// </summary>
        public static readonly string[] CanViewUsers = new[] { Administrator, Management };
        
        /// <summary>
        /// Roles that can manage users (create, edit, delete)
        /// </summary>
        public static readonly string[] CanManageUsers = new[] { Administrator };
        
        /// <summary>
        /// Helper method to check if a role is valid
        /// </summary>
        public static bool IsValid(string role)
        {
            return role == Administrator || role == Management || role == Monitor;
        }
        
        /// <summary>
        /// Helper method to get comma-separated roles for Authorize attribute
        /// </summary>
        public static string Combine(params string[] roles)
        {
            return string.Join(",", roles);
        }
    }
}
