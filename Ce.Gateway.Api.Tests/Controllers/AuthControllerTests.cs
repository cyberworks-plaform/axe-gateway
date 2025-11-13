using Ce.Gateway.Api.Controllers.Api;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models.Auth;
using Ce.Gateway.Api.Models.Common;
using Ce.Gateway.Api.Services.Interface;
using Ce.Gateway.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Ce.Gateway.Api.Tests.Controllers
{
    /// <summary>
    /// Unit tests for AuthController API
    /// </summary>
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockUserManager = IdentityTestHelper.CreateMockUserManager();
            _mockLogger = new Mock<ILogger<AuthController>>();

            _controller = new AuthController(
                _mockAuthService.Object,
                _mockUserManager.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Login_WithValidCredentials_Returns200AndToken()
        {
            // Arrange
            var request = new LoginRequest
            {
                Username = "testuser",
                Password = "Test@123"
            };

            var loginResponse = new LoginResponse
            {
                Token = "valid-jwt-token",
                Username = "testuser",
                FullName = "Test User",
                Email = "test@example.com",
                Role = Roles.Monitor
            };

            _mockAuthService
                .Setup(s => s.LoginAsync(request))
                .ReturnsAsync(loginResponse);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LoginResponse>>(okResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("testuser", response.Data.Username);
            Assert.NotNull(response.Data.Token);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_Returns401()
        {
            // Arrange
            var request = new LoginRequest
            {
                Username = "testuser",
                Password = "WrongPassword"
            };

            _mockAuthService
                .Setup(s => s.LoginAsync(request))
                .ReturnsAsync((LoginResponse)null);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LoginResponse>>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Login failed", response.Message);
        }

        [Fact]
        public async Task Login_WithInactiveUser_Returns401()
        {
            // Arrange
            var request = new LoginRequest
            {
                Username = "inactiveuser",
                Password = "Test@123"
            };

            _mockAuthService
                .Setup(s => s.LoginAsync(request))
                .ThrowsAsync(new UnauthorizedAccessException("User account is inactive"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LoginResponse>>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Login failed", response.Message);
        }

        [Fact]
        public async Task GetCurrentUser_WithAuthenticatedUser_Returns200WithUserInfo()
        {
            // Arrange
            var userId = "user-123";
            var user = IdentityTestHelper.CreateTestUser(userId, "testuser", "test@example.com", "Test User", true);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId)
                    }))
                }
            };

            _mockAuthService
                .Setup(s => s.GetCurrentUserAsync(userId))
                .ReturnsAsync(user);

            _mockUserManager
                .Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { Roles.Monitor });

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<UserDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("testuser", response.Data.Username);
            Assert.Equal("test@example.com", response.Data.Email);
        }

        [Fact]
        public async Task GetCurrentUser_WithoutAuthentication_Returns401()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<UserDto>>(unauthorizedResult.Value);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task GetCurrentUser_WithNonExistentUser_Returns404()
        {
            // Arrange
            var userId = "non-existent";
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId)
                    }))
                }
            };

            _mockAuthService
                .Setup(s => s.GetCurrentUserAsync(userId))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<UserDto>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal("User not found", response.Message);
        }
    }
}
