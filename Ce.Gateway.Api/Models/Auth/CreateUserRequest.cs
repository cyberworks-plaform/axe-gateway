using System.ComponentModel.DataAnnotations;

namespace Ce.Gateway.Api.Models.Auth
{
    public class CreateUserRequest
    {
        [Required]
        [MaxLength(100)]
        public string Username { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [MaxLength(100)]
        public string FullName { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string Role { get; set; } // Administrator, Management, Monitor
    }
}
