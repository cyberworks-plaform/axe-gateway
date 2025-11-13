using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Controllers.Pages
{
    [Authorize(Roles = "Administrator,Management")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var userViewModels = new System.Collections.Generic.List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserDto
                {
                    Id = user.Id,
                    Username = user.UserName,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = roles.FirstOrDefault(),
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                });
            }

            return View(userViewModels);
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserRequest model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Username,
                    Email = model.Email,
                    FullName = model.FullName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }

                    TempData["Success"] = "User created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var model = new UpdateUserRequest
            {
                FullName = user.FullName,
                Email = user.Email,
                Role = roles.FirstOrDefault(),
                IsActive = user.IsActive
            };

            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.UserId = id;
            ViewBag.Username = user.UserName;

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UpdateUserRequest model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.IsActive = model.IsActive ?? true;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // Update role if changed
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        var currentRoles = await _userManager.GetRolesAsync(user);
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }

                    // Change password if provided
                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                        await _userManager.ResetPasswordAsync(user, token, model.Password);
                    }

                    TempData["Success"] = "User updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.UserId = id;
            ViewBag.Username = user.UserName;

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var user = await _userManager.FindByIdAsync(id);
                
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if trying to delete self
                if (currentUser != null && id == currentUser.Id)
                {
                    TempData["Error"] = "Cannot delete your own account.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if trying to delete last admin
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains(Roles.Administrator))
                {
                    var activeAdmins = await _userManager.Users
                        .Where(u => u.IsActive)
                        .ToListAsync();

                    var adminCount = 0;
                    foreach (var u in activeAdmins)
                    {
                        var userRoles = await _userManager.GetRolesAsync(u);
                        if (userRoles.Contains(Roles.Administrator))
                        {
                            adminCount++;
                        }
                    }

                    if (adminCount <= 1)
                    {
                        TempData["Error"] = "Cannot delete the last active administrator.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    TempData["Success"] = "User deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to delete user: " + string.Join(", ", result.Errors.Select(e => e.Description));
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting user: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
