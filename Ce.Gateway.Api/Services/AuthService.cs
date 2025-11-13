using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models.Auth;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services
{
    /// <summary>
    /// Service for authentication and JWT token management
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user and generate JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Login response with JWT token and user information</returns>
        /// <exception cref="InvalidOperationException">Thrown when credentials are invalid or user is inactive</exception>
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            _logger.LogInformation("Login attempt for user: {Username}", request.Username);

            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found - {Username}", request.Username);
                throw new InvalidOperationException("Invalid username or password");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: User is inactive - {Username}", request.Username);
                throw new InvalidOperationException("Your account has been disabled. Please contact administrator.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            
            if (result.IsLockedOut)
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var remainingMinutes = lockoutEnd.HasValue 
                    ? Math.Ceiling((lockoutEnd.Value - DateTimeOffset.Now).TotalMinutes) 
                    : 5;
                
                _logger.LogWarning("Login failed: User is locked out - {Username}", request.Username);
                throw new InvalidOperationException(
                    $"Account locked due to multiple failed login attempts. Try again in {remainingMinutes} minutes.");
            }

            if (!result.Succeeded)
            {
                _logger.LogWarning("Login failed: Invalid password - {Username}", request.Username);
                throw new InvalidOperationException("Invalid username or password");
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.Count > 0 ? roles[0] : Roles.Monitor;

            var token = GenerateJwtToken(user, role);

            _logger.LogInformation("Login successful for user: {Username}", request.Username);

            return new LoginResponse
            {
                Token = token,
                Username = user.UserName,
                FullName = user.FullName,
                Role = role,
                Email = user.Email
            };
        }

        /// <summary>
        /// Validate JWT token
        /// </summary>
        /// <param name="token">JWT token to validate</param>
        /// <returns>True if token is valid</returns>
        public async Task<bool> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Tokens:Key"]);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Tokens:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Tokens:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return await Task.FromResult(false);
            }
        }

        /// <summary>
        /// Get current user by userId
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>Application user</returns>
        /// <exception cref="KeyNotFoundException">Thrown when user is not found</exception>
        public async Task<ApplicationUser> GetCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                throw new KeyNotFoundException($"User with ID {userId} not found");
            }

            return user;
        }

        /// <summary>
        /// Generate JWT token for authenticated user
        /// </summary>
        /// <param name="user">Application user</param>
        /// <param name="role">User role</param>
        /// <returns>JWT token string</returns>
        public string GenerateJwtToken(ApplicationUser user, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Tokens:Key"]);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, role),
                new Claim("userId", user.Id),
                new Claim("fullName", user.FullName ?? string.Empty)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["Tokens:Issuer"],
                Audience = _configuration["Tokens:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
