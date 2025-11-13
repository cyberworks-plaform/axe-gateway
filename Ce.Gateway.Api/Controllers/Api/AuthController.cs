using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models.Auth;
using Ce.Gateway.Api.Models.Common;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Controllers.Api
{
    /// <summary>
    /// Authentication API controller for user login and token management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService, 
            UserManager<ApplicationUser> userManager,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates user and returns JWT token
        /// </summary>
        /// <param name="request">Login credentials (username and password)</param>
        /// <returns>JWT token and user information</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Invalid request", "Please provide valid username and password"));
                }

                _logger.LogInformation("Login attempt for user: {Username}", request.Username);

                var loginResponse = await _authService.LoginAsync(request);

                if (loginResponse == null)
                {
                    _logger.LogWarning("Login failed for user: {Username}", request.Username);
                    return Unauthorized(ApiResponse<LoginResponse>.ErrorResult("Login failed", "Invalid username or password"));
                }

                _logger.LogInformation("User {Username} logged in successfully", request.Username);
                return Ok(ApiResponse<LoginResponse>.SuccessResult(loginResponse, "Login successful"));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized login attempt for user: {Username}", request.Username);
                return Unauthorized(ApiResponse<LoginResponse>.ErrorResult("Login failed", ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
                return StatusCode(500, ApiResponse<LoginResponse>.ErrorResult("Internal server error", "An error occurred during login"));
            }
        }

        /// <summary>
        /// Gets current authenticated user information
        /// </summary>
        /// <returns>Current user details</returns>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unable to extract user ID from token");
                    return Unauthorized(ApiResponse<UserDto>.ErrorResult("Invalid token", "Unable to identify user from token"));
                }

                var user = await _authService.GetCurrentUserAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return NotFound(ApiResponse<UserDto>.ErrorResult("User not found", "Current user does not exist"));
                }

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? Roles.Monitor;

                var userDto = new UserDto
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

                return Ok(ApiResponse<UserDto>.SuccessResult(userDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user information");
                return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Internal server error", "An error occurred while retrieving user information"));
            }
        }
    }
}
