using Ce.Gateway.Api.Controllers.Api;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Models.Auth;
using Ce.Gateway.Api.Models.Common;
using Ce.Gateway.Api.Services.Interface;
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
    /// Unit tests for UsersController API
    /// </summary>
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ILogger<UsersController>> _mockLogger;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<UsersController>>();

            _controller = new UsersController(
                _mockUserService.Object,
                _mockLogger.Object);
        }

        private void SetupUserContext(string userId, string role)
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId),
                        new Claim(ClaimTypes.Role, role)
                    }))
                }
            };
        }

        [Fact]
        public async Task GetUsers_AsAdmin_Returns200WithUsers()
        {
            // Arrange
            SetupUserContext("admin-id", Roles.Administrator);

            var users = new PaginatedResult<UserDto>
            {
                Data = new List<UserDto>
                {
                    new UserDto { Id = "1", Username = "user1", Role = Roles.Monitor },
                    new UserDto { Id = "2", Username = "user2", Role = Roles.Management }
                },
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _mockUserService
                .Setup(s => s.GetUsersAsync(1, 10))
                .ReturnsAsync(users);

            // Act
            var result = await _controller.GetUsers(1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<PaginatedResult<UserDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal(2, response.Data.TotalCount);
        }

        [Fact]
        public async Task GetUserById_AsAdmin_Returns200()
        {
            // Arrange
            SetupUserContext("admin-id", Roles.Administrator);

            var userDto = new UserDto
            {
                Id = "user-123",
                Username = "testuser",
                Email = "test@example.com",
                Role = Roles.Monitor
            };

            _mockUserService
                .Setup(s => s.GetUserByIdAsync("user-123"))
                .ReturnsAsync(userDto);

            // Act
            var result = await _controller.GetUser("user-123");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<UserDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("testuser", response.Data.Username);
        }

        [Fact]
        public async Task GetUserById_WithNonExistentUser_Returns404()
        {
            // Arrange
            SetupUserContext("admin-id", Roles.Administrator);

            _mockUserService
                .Setup(s => s.GetUserByIdAsync("non-existent"))
                .ReturnsAsync((UserDto)null);

            // Act
            var result = await _controller.GetUser("non-existent");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<UserDto>>(notFoundResult.Value);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task CreateUser_AsAdmin_Returns201()
        {
            // Arrange
            SetupUserContext("admin-id", Roles.Administrator);

            var createRequest = new CreateUserRequest
            {
                Username = "newuser",
                Email = "new@example.com",
                FullName = "New User",
                Password = "Test@123",
                Role = Roles.Monitor
            };

            var createdUser = new UserDto
            {
                Id = "new-user-id",
                Username = "newuser",
                Email = "new@example.com",
                FullName = "New User",
                Role = Roles.Monitor
            };

            _mockUserService
                .Setup(s => s.CreateUserAsync(createRequest, "admin-id"))
                .ReturnsAsync(createdUser);

            // Act
            var result = await _controller.CreateUser(createRequest);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<ApiResponse<UserDto>>(createdResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("newuser", response.Data.Username);
        }

        [Fact]
        public async Task CreateUser_WithDuplicateUsername_Returns400()
        {
            // Arrange
            SetupUserContext("admin-id", Roles.Administrator);

            var createRequest = new CreateUserRequest
            {
                Username = "existinguser",
                Email = "new@example.com",
                FullName = "New User",
                Password = "Test@123",
                Role = Roles.Monitor
            };

            _mockUserService
                .Setup(s => s.CreateUserAsync(createRequest, "admin-id"))
                .ThrowsAsync(new InvalidOperationException("Username already exists"));

            // Act
            var result = await _controller.CreateUser(createRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<UserDto>>(badRequestResult.Value);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task UpdateUser_AsAdmin_Returns200()
        {
            // Arrange
            SetupUserContext("admin-id", Roles.Administrator);

            var updateRequest = new UpdateUserRequest
            {
                FullName = "Updated Name",
                Email = "updated@example.com",
                Role = Roles.Management,
                IsActive = true
            };

            var updatedUser = new UserDto
            {
                Id = "user-123",
                Username = "testuser",
                FullName = "Updated Name",
                Email = "updated@example.com",
                Role = Roles.Management
            };

            _mockUserService
                .Setup(s => s.UpdateUserAsync("user-123", updateRequest, "admin-id"))
                .ReturnsAsync(updatedUser);

            // Act
            var result = await _controller.UpdateUser("user-123", updateRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<UserDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal("Updated Name", response.Data.FullName);
        }

        [Fact]
        public async Task DeleteUser_RootAdmin_Returns400()
        {
            // Arrange
            SetupUserContext("admin-id", Roles.Administrator);

            _mockUserService
                .Setup(s => s.DeleteUserAsync("root-admin-id", "admin-id"))
                .ThrowsAsync(new InvalidOperationException("Cannot delete root administrator"));

            // Act
            var result = await _controller.DeleteUser("root-admin-id");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<bool>>(badRequestResult.Value);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task DeleteUser_LastAdmin_Returns400()
        {
            // Arrange
            SetupUserContext("admin-id", Roles.Administrator);

            _mockUserService
                .Setup(s => s.DeleteUserAsync("last-admin-id", "admin-id"))
                .ThrowsAsync(new InvalidOperationException("Cannot delete the last administrator"));

            // Act
            var result = await _controller.DeleteUser("last-admin-id");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<bool>>(badRequestResult.Value);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task DeleteUser_ValidUser_Returns200()
        {
            // Arrange
            SetupUserContext("admin-id", Roles.Administrator);

            _mockUserService
                .Setup(s => s.DeleteUserAsync("user-123", "admin-id"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteUser("user-123");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
            Assert.True(response.Success);
            Assert.True(response.Data);
        }

        [Fact]
        public async Task ChangePassword_AsAdmin_Returns204()
        {
            // Arrange
            SetupUserContext("admin-id", Roles.Administrator);

            var changePasswordRequest = new ChangePasswordRequest
            {
                NewPassword = "NewTest@123"
            };

            _mockUserService
                .Setup(s => s.ChangePasswordAsync("user-123", changePasswordRequest.NewPassword, "admin-id"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ChangePassword("user-123", changePasswordRequest);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task ChangePassword_WithNonExistentUser_Returns404()
        {
            // Arrange
            SetupUserContext("admin-id", Roles.Administrator);

            var changePasswordRequest = new ChangePasswordRequest
            {
                NewPassword = "NewTest@123"
            };

            _mockUserService
                .Setup(s => s.ChangePasswordAsync("non-existent", changePasswordRequest.NewPassword, "admin-id"))
                .ThrowsAsync(new InvalidOperationException("User not found"));

            // Act
            var result = await _controller.ChangePassword("non-existent", changePasswordRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<bool>>(badRequestResult.Value);
            Assert.False(response.Success);
        }
    }
}
