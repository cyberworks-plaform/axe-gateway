using System.ComponentModel.DataAnnotations;

namespace Ce.Gateway.Api.Models.Auth
{
    public class ChangePasswordRequest
    {
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }
    }
}
