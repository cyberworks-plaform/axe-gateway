using System.ComponentModel.DataAnnotations;

namespace Ce.Gateway.Api.Models.Auth
{
    public class UpdateUserRequest
    {
        [MaxLength(100)]
        public string FullName { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        public string Role { get; set; }

        public bool? IsActive { get; set; }

        [MinLength(6)]
        public string Password { get; set; } // Optional - only if changing password
    }
}
