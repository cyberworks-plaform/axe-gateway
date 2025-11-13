using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Controllers.Pages
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequest model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return View(model);
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Inactive user login attempt: {Username}", model.Username);
                    ModelState.AddModelError(string.Empty, "Your account has been disabled. Please contact administrator.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, isPersistent: false, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    // Update last login time
                    user.LastLoginAt = System.DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    _logger.LogInformation("User logged in successfully: {Username}", model.Username);
                    return RedirectToLocal(returnUrl);
                }
                if (result.IsLockedOut)
                {
                    var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                    var remainingMinutes = lockoutEnd.HasValue 
                        ? Math.Ceiling((lockoutEnd.Value - System.DateTimeOffset.Now).TotalMinutes) 
                        : 5;
                    
                    _logger.LogWarning("Locked out user login attempt: {Username}", model.Username);
                    ModelState.AddModelError(string.Empty, 
                        $"Account locked due to multiple failed login attempts. Try again in {remainingMinutes} minutes.");
                    return View(model);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Display current user profile
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found when accessing profile");
                return RedirectToAction(nameof(Login));
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Role = roles.Count > 0 ? roles[0] : "Unknown";
            
            return View(user);
        }

        /// <summary>
        /// Display change password form
        /// </summary>
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        /// <summary>
        /// Change password for current user
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordCurrentUserRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found when changing password");
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("User {Username} changed password successfully", user.UserName);
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Password changed successfully";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            _logger.LogWarning("Failed to change password for user {Username}", user.UserName);
            return View(model);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Dashboard");
            }
        }
    }
}
