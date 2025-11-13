using System.ComponentModel.DataAnnotations;

namespace Ce.Gateway.Api.Models.Auth
{
    /// <summary>
    /// Request model for changing current user's password
    /// </summary>
    public class ChangePasswordCurrentUserRequest
    {
        [Required(ErrorMessage = "Current password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }
        
        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }
        
        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation password do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }
    }
}
