using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Models.Auth;
using Ce.Gateway.Api.Models.Common;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Controllers.Api
{
    /// <summary>
    /// User management API controller for CRUD operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all users with pagination
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10)</param>
        /// <returns>Paginated list of users</returns>
        [HttpGet]
        [Authorize(Roles = $"{Roles.Administrator},{Roles.Management}")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<UserDto>>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1)
                {
                    return BadRequest(ApiResponse<PaginatedResult<UserDto>>.ErrorResult("Invalid page number", "Page number must be greater than 0"));
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(ApiResponse<PaginatedResult<UserDto>>.ErrorResult("Invalid page size", "Page size must be between 1 and 100"));
                }

                var users = await _userService.GetUsersAsync(page, pageSize);
                return Ok(ApiResponse<PaginatedResult<UserDto>>.SuccessResult(users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users list");
                return StatusCode(500, ApiResponse<PaginatedResult<UserDto>>.ErrorResult("Internal server error", "An error occurred while retrieving users"));
            }
        }

        /// <summary>
        /// Gets a specific user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = $"{Roles.Administrator},{Roles.Management}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResult("Invalid user ID", "User ID cannot be empty"));
                }

                var user = await _userService.GetUserByIdAsync(id);
                return Ok(ApiResponse<UserDto>.SuccessResult(user));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found: {UserId}", id);
                return NotFound(ApiResponse<UserDto>.ErrorResult("User not found", ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user: {UserId}", id);
                return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Internal server error", "An error occurred while retrieving user"));
            }
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="request">User creation data</param>
        /// <returns>Created user details</returns>
        [HttpPost]
        [Authorize(Roles = Roles.Administrator)]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResult("Invalid request", "Please provide all required fields"));
                }

                if (!Roles.IsValid(request.Role))
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResult("Invalid role", $"Role must be one of: {Roles.Administrator}, {Roles.Management}, {Roles.Monitor}"));
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                var user = await _userService.CreateUserAsync(request, currentUserId);

                _logger.LogInformation("User created: {UserId} by {CreatedBy}", user.Id, currentUserId);

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ApiResponse<UserDto>.SuccessResult(user, "User created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to create user: {Username}", request.Username);
                return BadRequest(ApiResponse<UserDto>.ErrorResult("Failed to create user", ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", request.Username);
                return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Internal server error", "An error occurred while creating user"));
            }
        }

        /// <summary>
        /// Updates an existing user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="request">User update data</param>
        /// <returns>Updated user details</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Administrator)]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResult("Invalid request", "Please provide valid data"));
                }

                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResult("Invalid user ID", "User ID cannot be empty"));
                }

                if (!string.IsNullOrEmpty(request.Role) && !Roles.IsValid(request.Role))
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResult("Invalid role", $"Role must be one of: {Roles.Administrator}, {Roles.Management}, {Roles.Monitor}"));
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                var user = await _userService.UpdateUserAsync(id, request, currentUserId);

                _logger.LogInformation("User updated: {UserId} by {UpdatedBy}", id, currentUserId);

                return Ok(ApiResponse<UserDto>.SuccessResult(user, "User updated successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found: {UserId}", id);
                return NotFound(ApiResponse<UserDto>.ErrorResult("User not found", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to update user: {UserId}", id);
                return BadRequest(ApiResponse<UserDto>.ErrorResult("Failed to update user", ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", id);
                return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Internal server error", "An error occurred while updating user"));
            }
        }

        /// <summary>
        /// Deletes a user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>No content on success</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Administrator)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteUser(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(ApiResponse<object>.ErrorResult("Invalid user ID", "User ID cannot be empty"));
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                var canDelete = await _userService.CanDeleteUserAsync(id, currentUserId);

                if (!canDelete)
                {
                    return StatusCode(403, ApiResponse<object>.ErrorResult("Cannot delete user", "You cannot delete yourself or the root administrator"));
                }

                var result = await _userService.DeleteUserAsync(id, currentUserId, currentUserId);

                if (!result)
                {
                    return NotFound(ApiResponse<object>.ErrorResult("User not found", "User does not exist"));
                }

                _logger.LogInformation("User deleted: {UserId} by {DeletedBy}", id, currentUserId);

                return Ok(ApiResponse<object>.SuccessResult(null, "User deleted successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found: {UserId}", id);
                return NotFound(ApiResponse<object>.ErrorResult("User not found", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to delete user: {UserId}", id);
                return StatusCode(403, ApiResponse<object>.ErrorResult("Cannot delete user", ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                return StatusCode(500, ApiResponse<object>.ErrorResult("Internal server error", "An error occurred while deleting user"));
            }
        }

        /// <summary>
        /// Changes user password
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="request">New password data</param>
        /// <returns>Success message</returns>
        [HttpPost("{id}/change-password")]
        [Authorize(Roles = Roles.Administrator)]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword(string id, [FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.ErrorResult("Invalid request", "Please provide a valid password"));
                }

                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(ApiResponse<object>.ErrorResult("Invalid user ID", "User ID cannot be empty"));
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                var result = await _userService.ChangePasswordAsync(id, request.NewPassword, currentUserId);

                if (!result)
                {
                    return NotFound(ApiResponse<object>.ErrorResult("User not found", "User does not exist"));
                }

                _logger.LogInformation("Password changed for user: {UserId} by {ChangedBy}", id, currentUserId);

                return Ok(ApiResponse<object>.SuccessResult(null, "Password changed successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found: {UserId}", id);
                return NotFound(ApiResponse<object>.ErrorResult("User not found", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to change password for user: {UserId}", id);
                return BadRequest(ApiResponse<object>.ErrorResult("Failed to change password", ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", id);
                return StatusCode(500, ApiResponse<object>.ErrorResult("Internal server error", "An error occurred while changing password"));
            }
        }

        /// <summary>
        /// Checks if a user can be deleted
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>True if user can be deleted, false otherwise</returns>
        [HttpGet("{id}/can-delete")]
        [Authorize(Roles = Roles.Administrator)]
        public async Task<ActionResult<ApiResponse<bool>>> CanDeleteUser(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(ApiResponse<bool>.ErrorResult("Invalid user ID", "User ID cannot be empty"));
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                var canDelete = await _userService.CanDeleteUserAsync(id, currentUserId);

                return Ok(ApiResponse<bool>.SuccessResult(canDelete));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can be deleted: {UserId}", id);
                return StatusCode(500, ApiResponse<bool>.ErrorResult("Internal server error", "An error occurred while checking delete permission"));
            }
        }

        /// <summary>
        /// Unlocks a locked user account
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success message</returns>
        [HttpPost("{id}/unlock")]
        [Authorize(Roles = Roles.Administrator)]
        public async Task<ActionResult<ApiResponse<object>>> UnlockUser(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(ApiResponse<object>.ErrorResult("Invalid user ID", "User ID cannot be empty"));
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                
                var result = await _userService.UnlockUserAsync(id, currentUserId);

                if (!result)
                {
                    return NotFound(ApiResponse<object>.ErrorResult("User not found", "User does not exist"));
                }

                _logger.LogInformation("User {UserId} unlocked by {AdminId}", id, currentUserId);

                return Ok(ApiResponse<object>.SuccessResult(null, "User unlocked successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found: {UserId}", id);
                return NotFound(ApiResponse<object>.ErrorResult("User not found", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to unlock user: {UserId}", id);
                return BadRequest(ApiResponse<object>.ErrorResult("Failed to unlock user", ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user: {UserId}", id);
                return StatusCode(500, ApiResponse<object>.ErrorResult("Internal server error", "An error occurred while unlocking the user"));
            }
        }
    }
}
