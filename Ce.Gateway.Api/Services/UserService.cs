using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Models.Auth;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services
{
    /// <summary>
    /// Service for user management operations
    /// </summary>
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated list of users with their roles
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Paginated result of users</returns>
        public async Task<PaginatedResult<UserDto>> GetUsersAsync(int page, int pageSize)
        {
            var query = _userManager.Users.AsQueryable();

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? Roles.Monitor;

                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Username = user.UserName,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                });
            }

            return new PaginatedResult<UserDto>
            {
                TotalCount = totalCount,
                TotalPages = totalPages,
                Page = page,
                PageSize = pageSize,
                Data = userDtos
            };
        }

        /// <summary>
        /// Get single user by ID with role information
        /// </summary>
        /// <param name="id">User identifier</param>
        /// <returns>User details</returns>
        /// <exception cref="KeyNotFoundException">Thrown when user is not found</exception>
        public async Task<UserDto> GetUserByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", id);
                throw new KeyNotFoundException($"User with ID {id} not found");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? Roles.Monitor;

            return new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                Role = role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        /// <summary>
        /// Create new user with role assignment
        /// </summary>
        /// <param name="request">User creation request</param>
        /// <param name="createdBy">Username of creator</param>
        /// <returns>Created user details</returns>
        /// <exception cref="InvalidOperationException">Thrown when validation fails or user already exists</exception>
        public async Task<UserDto> CreateUserAsync(CreateUserRequest request, string createdBy)
        {
            _logger.LogInformation("Creating user: {Username} by {CreatedBy}", request.Username, createdBy);

            if (!Roles.IsValid(request.Role))
            {
                throw new InvalidOperationException($"Invalid role: {request.Role}");
            }

            var existingUser = await _userManager.FindByNameAsync(request.Username);
            if (existingUser != null)
            {
                throw new InvalidOperationException($"Username {request.Username} already exists");
            }

            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                FullName = request.FullName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create user {Username}: {Errors}", request.Username, errors);
                throw new InvalidOperationException($"Failed to create user: {errors}");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign role to user {Username}: {Errors}", request.Username, errors);
                await _userManager.DeleteAsync(user);
                throw new InvalidOperationException($"Failed to assign role: {errors}");
            }

            _logger.LogInformation("User created successfully: {Username}", request.Username);

            return new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                Role = request.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        /// <summary>
        /// Update user details including role and password
        /// </summary>
        /// <param name="id">User identifier</param>
        /// <param name="request">Update request</param>
        /// <param name="updatedBy">Username of updater</param>
        /// <returns>Updated user details</returns>
        /// <exception cref="KeyNotFoundException">Thrown when user is not found</exception>
        /// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
        public async Task<UserDto> UpdateUserAsync(string id, UpdateUserRequest request, string updatedBy)
        {
            _logger.LogInformation("Updating user: {UserId} by {UpdatedBy}", id, updatedBy);

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {id} not found");
            }

            if (!string.IsNullOrEmpty(request.Role) && !Roles.IsValid(request.Role))
            {
                throw new InvalidOperationException($"Invalid role: {request.Role}");
            }

            if (!string.IsNullOrEmpty(request.FullName))
            {
                user.FullName = request.FullName;
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                user.Email = request.Email;
            }

            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to update user {UserId}: {Errors}", id, errors);
                throw new InvalidOperationException($"Failed to update user: {errors}");
            }

            if (!string.IsNullOrEmpty(request.Role))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Count > 0)
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }
                await _userManager.AddToRoleAsync(user, request.Role);
            }

            if (!string.IsNullOrEmpty(request.Password))
            {
                var removePasswordResult = await _userManager.RemovePasswordAsync(user);
                if (removePasswordResult.Succeeded)
                {
                    var addPasswordResult = await _userManager.AddPasswordAsync(user, request.Password);
                    if (!addPasswordResult.Succeeded)
                    {
                        var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to change password for user {UserId}: {Errors}", id, errors);
                        throw new InvalidOperationException($"Failed to change password: {errors}");
                    }
                }
            }

            _logger.LogInformation("User updated successfully: {UserId}", id);

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? Roles.Monitor;

            return new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                Role = role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        /// <summary>
        /// Delete user with business rule validation
        /// </summary>
        /// <param name="id">User identifier</param>
        /// <param name="deletedBy">Username of deleter</param>
        /// <param name="currentUserId">Current user's ID to prevent self-deletion</param>
        /// <returns>True if deletion succeeded</returns>
        /// <exception cref="KeyNotFoundException">Thrown when user is not found</exception>
        /// <exception cref="InvalidOperationException">Thrown when business rules prevent deletion</exception>
        public async Task<bool> DeleteUserAsync(string id, string deletedBy, string currentUserId = null)
        {
            _logger.LogInformation("Deleting user: {UserId} by {DeletedBy}", id, deletedBy);

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {id} not found");
            }

            if (!string.IsNullOrEmpty(currentUserId) && id == currentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to delete themselves", id);
                throw new InvalidOperationException("Cannot delete your own account");
            }

            if (await IsRootAdminAsync(id))
            {
                throw new InvalidOperationException("Cannot delete root administrator account");
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(Roles.Administrator))
            {
                var activeAdminCount = await _userManager.Users
                    .Where(u => u.IsActive)
                    .ToListAsync();

                var adminCount = 0;
                foreach (var u in activeAdminCount)
                {
                    var userRoles = await _userManager.GetRolesAsync(u);
                    if (userRoles.Contains(Roles.Administrator))
                    {
                        adminCount++;
                    }
                }

                if (adminCount <= 1)
                {
                    _logger.LogWarning("Cannot delete last active administrator: {UserId}", id);
                    throw new InvalidOperationException("Cannot delete the last active administrator");
                }
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to delete user {UserId}: {Errors}", id, errors);
                throw new InvalidOperationException($"Failed to delete user: {errors}");
            }

            _logger.LogInformation("User deleted successfully: {UserId}", id);
            return true;
        }

        /// <summary>
        /// Check if user can be deleted based on business rules
        /// </summary>
        /// <param name="id">User identifier to check</param>
        /// <param name="currentUserId">Current user identifier performing the check</param>
        /// <returns>True if user can be deleted</returns>
        public async Task<bool> CanDeleteUserAsync(string id, string currentUserId)
        {
            if (id == currentUserId)
            {
                return false;
            }

            if (await IsRootAdminAsync(id))
            {
                return false;
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return false;
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(Roles.Administrator))
            {
                var activeAdminCount = await _userManager.Users
                    .Where(u => u.IsActive)
                    .ToListAsync();

                var adminCount = 0;
                foreach (var u in activeAdminCount)
                {
                    var userRoles = await _userManager.GetRolesAsync(u);
                    if (userRoles.Contains(Roles.Administrator))
                    {
                        adminCount++;
                    }
                }

                if (adminCount <= 1)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get count of active users
        /// </summary>
        /// <returns>Number of active users</returns>
        public async Task<int> GetActiveUserCountAsync()
        {
            return await _userManager.Users.CountAsync(u => u.IsActive);
        }

        /// <summary>
        /// Check if user is root administrator
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>True if user is root admin (username = "admin")</returns>
        public async Task<bool> IsRootAdminAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.UserName?.Equals("admin", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="id">User identifier</param>
        /// <param name="newPassword">New password</param>
        /// <param name="changedBy">Username of person changing password</param>
        /// <returns>True if password change succeeded</returns>
        /// <exception cref="KeyNotFoundException">Thrown when user is not found</exception>
        /// <exception cref="InvalidOperationException">Thrown when password change fails</exception>
        public async Task<bool> ChangePasswordAsync(string id, string newPassword, string changedBy)
        {
            _logger.LogInformation("Changing password for user: {UserId} by {ChangedBy}", id, changedBy);

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {id} not found");
            }

            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                var errors = string.Join(", ", removePasswordResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to remove old password for user {UserId}: {Errors}", id, errors);
                throw new InvalidOperationException($"Failed to remove old password: {errors}");
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, newPassword);
            if (!addPasswordResult.Succeeded)
            {
                var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to set new password for user {UserId}: {Errors}", id, errors);
                throw new InvalidOperationException($"Failed to set new password: {errors}");
            }

            _logger.LogInformation("Password changed successfully for user: {UserId}", id);
            return true;
        }
    }
}
