using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models.Auth;
using Ce.Gateway.Api.Services;
using Ce.Gateway.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Ce.Gateway.Api.Tests.Services
{
    /// <summary>
    /// Unit tests for AuthService
    /// </summary>
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private readonly IConfiguration _configuration;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockUserManager = IdentityTestHelper.CreateMockUserManager();
            _mockRoleManager = IdentityTestHelper.CreateMockRoleManager();
            _mockSignInManager = IdentityTestHelper.CreateMockSignInManager(_mockUserManager.Object);
            _configuration = IdentityTestHelper.CreateTestConfiguration();
            _mockLogger = new Mock<ILogger<AuthService>>();

            _authService = new AuthService(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockRoleManager.Object,
                _configuration,
                _mockLogger.Object);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndUserInfo()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com", "Test User", true);
            var request = new LoginRequest
            {
                Username = "testuser",
                Password = "Test@123"
            };

            _mockUserManager
                .Setup(um => um.FindByNameAsync(request.Username))
                .ReturnsAsync(user);

            _mockSignInManager
                .Setup(sm => sm.CheckPasswordSignInAsync(user, request.Password, true))
                .ReturnsAsync(SignInResult.Success);

            _mockUserManager
                .Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { Roles.Monitor });

            _mockUserManager
                .Setup(um => um.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.Equal("testuser", result.Username);
            Assert.Equal("Test User", result.FullName);
            Assert.Equal(Roles.Monitor, result.Role);
            Assert.Equal("test@example.com", result.Email);

            _mockUserManager.Verify(um => um.UpdateAsync(It.Is<ApplicationUser>(u => u.LastLoginAt != null)), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidUsername_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new LoginRequest
            {
                Username = "nonexistent",
                Password = "Test@123"
            };

            _mockUserManager
                .Setup(um => um.FindByNameAsync(request.Username))
                .ReturnsAsync((ApplicationUser)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.LoginAsync(request));

            Assert.Equal("Invalid username or password", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com", "Test User", true);
            var request = new LoginRequest
            {
                Username = "testuser",
                Password = "WrongPassword"
            };

            _mockUserManager
                .Setup(um => um.FindByNameAsync(request.Username))
                .ReturnsAsync(user);

            _mockSignInManager
                .Setup(sm => sm.CheckPasswordSignInAsync(user, request.Password, true))
                .ReturnsAsync(SignInResult.Failed);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.LoginAsync(request));

            Assert.Equal("Invalid username or password", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_WithInactiveUser_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com", "Test User", false);
            var request = new LoginRequest
            {
                Username = "testuser",
                Password = "Test@123"
            };

            _mockUserManager
                .Setup(um => um.FindByNameAsync(request.Username))
                .ReturnsAsync(user);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.LoginAsync(request));

            Assert.Equal("Your account has been disabled. Please contact administrator.", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_WithLockedOutUser_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com", "Test User", true);
            var request = new LoginRequest
            {
                Username = "testuser",
                Password = "WrongPassword"
            };
            
            var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5);

            _mockUserManager
                .Setup(um => um.FindByNameAsync(request.Username))
                .ReturnsAsync(user);

            _mockSignInManager
                .Setup(sm => sm.CheckPasswordSignInAsync(user, request.Password, true))
                .ReturnsAsync(SignInResult.LockedOut);

            _mockUserManager
                .Setup(um => um.GetLockoutEndDateAsync(user))
                .ReturnsAsync(lockoutEnd);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.LoginAsync(request));

            Assert.Contains("Account locked due to multiple failed login attempts", exception.Message);
            Assert.Contains("minutes", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_UpdatesLastLoginTime()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com", "Test User", true);
            var request = new LoginRequest
            {
                Username = "testuser",
                Password = "Test@123"
            };

            DateTime? capturedLastLoginTime = null;

            _mockUserManager
                .Setup(um => um.FindByNameAsync(request.Username))
                .ReturnsAsync(user);

            _mockSignInManager
                .Setup(sm => sm.CheckPasswordSignInAsync(user, request.Password, true))
                .ReturnsAsync(SignInResult.Success);

            _mockUserManager
                .Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { Roles.Monitor });

            _mockUserManager
                .Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
                .Callback<ApplicationUser>(u => capturedLastLoginTime = u.LastLoginAt)
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _authService.LoginAsync(request);

            // Assert
            Assert.NotNull(capturedLastLoginTime);
            Assert.True((DateTime.UtcNow - capturedLastLoginTime.Value).TotalSeconds < 5);
        }

        [Fact]
        public async Task GetCurrentUserAsync_WithValidUserId_ReturnsUser()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com", "Test User", true);

            _mockUserManager
                .Setup(um => um.FindByIdAsync("user-id"))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.GetCurrentUserAsync("user-id");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("user-id", result.Id);
            Assert.Equal("testuser", result.UserName);
        }

        [Fact]
        public async Task GetCurrentUserAsync_WithInvalidUserId_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockUserManager
                .Setup(um => um.FindByIdAsync("invalid-id"))
                .ReturnsAsync((ApplicationUser)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _authService.GetCurrentUserAsync("invalid-id"));

            Assert.Contains("User with ID invalid-id not found", exception.Message);
        }

        [Fact]
        public void GenerateJwtToken_CreatesValidToken_WithCorrectClaims()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com", "Test User", true);
            var role = Roles.Administrator;

            // Act
            var token = _authService.GenerateJwtToken(user, role);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            // Validate token structure
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Verify claims
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user-id");
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Name && c.Value == "testuser");
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Email && c.Value == "test@example.com");
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == Roles.Administrator);
            Assert.Contains(jwtToken.Claims, c => c.Type == "userId" && c.Value == "user-id");
            Assert.Contains(jwtToken.Claims, c => c.Type == "fullName" && c.Value == "Test User");

            // Verify token properties
            Assert.Equal("TestIssuer", jwtToken.Issuer);
            Assert.Contains("TestAudience", jwtToken.Audiences);
            Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithValidToken_ReturnsTrue()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com", "Test User", true);
            var token = _authService.GenerateJwtToken(user, Roles.Monitor);

            // Act
            var result = await _authService.ValidateTokenAsync(token);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            var invalidToken = "invalid.token.string";

            // Act
            var result = await _authService.ValidateTokenAsync(invalidToken);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithEmptyToken_ReturnsFalse()
        {
            // Arrange
            var emptyToken = string.Empty;

            // Act
            var result = await _authService.ValidateTokenAsync(emptyToken);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithNullToken_ReturnsFalse()
        {
            // Arrange
            string nullToken = null;

            // Act
            var result = await _authService.ValidateTokenAsync(nullToken);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task LoginAsync_WithAdministratorRole_ReturnsCorrectRole()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("admin-id", "admin", "admin@example.com", "Administrator", true);
            var request = new LoginRequest
            {
                Username = "admin",
                Password = "Admin@123"
            };

            _mockUserManager
                .Setup(um => um.FindByNameAsync(request.Username))
                .ReturnsAsync(user);

            _mockSignInManager
                .Setup(sm => sm.CheckPasswordSignInAsync(user, request.Password, true))
                .ReturnsAsync(SignInResult.Success);

            _mockUserManager
                .Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { Roles.Administrator });

            _mockUserManager
                .Setup(um => um.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            Assert.Equal(Roles.Administrator, result.Role);
        }

        [Fact]
        public async Task LoginAsync_WithNoRole_DefaultsToMonitor()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com", "Test User", true);
            var request = new LoginRequest
            {
                Username = "testuser",
                Password = "Test@123"
            };

            _mockUserManager
                .Setup(um => um.FindByNameAsync(request.Username))
                .ReturnsAsync(user);

            _mockSignInManager
                .Setup(sm => sm.CheckPasswordSignInAsync(user, request.Password, true))
                .ReturnsAsync(SignInResult.Success);

            _mockUserManager
                .Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string>());

            _mockUserManager
                .Setup(um => um.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            Assert.Equal(Roles.Monitor, result.Role);
        }
    }
}
