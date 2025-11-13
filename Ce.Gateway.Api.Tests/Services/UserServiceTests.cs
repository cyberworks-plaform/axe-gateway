using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models.Auth;
using Ce.Gateway.Api.Services;
using Ce.Gateway.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Ce.Gateway.Api.Tests.Services
{
    /// <summary>
    /// Unit tests for UserService
    /// </summary>
    public class UserServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockUserManager = IdentityTestHelper.CreateMockUserManager();
            _mockRoleManager = IdentityTestHelper.CreateMockRoleManager();
            _mockLogger = new Mock<ILogger<UserService>>();

            _userService = new UserService(
                _mockUserManager.Object,
                _mockRoleManager.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CreateUserAsync_WithValidData_CreatesUser()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                Username = "newuser",
                Password = "NewUser@123",
                FullName = "New User",
                Email = "newuser@example.com",
                Role = Roles.Monitor
            };

            _mockUserManager
                .Setup(um => um.FindByNameAsync(request.Username))
                .ReturnsAsync((ApplicationUser)null);

            _mockUserManager
                .Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager
                .Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), request.Role))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.CreateUserAsync(request, "admin");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("newuser", result.Username);
            Assert.Equal("New User", result.FullName);
            Assert.Equal("newuser@example.com", result.Email);
            Assert.Equal(Roles.Monitor, result.Role);
            Assert.True(result.IsActive);

            _mockUserManager.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>(), request.Password), Times.Once);
            _mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), request.Role), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_WithInvalidRole_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                Username = "newuser",
                Password = "NewUser@123",
                FullName = "New User",
                Email = "newuser@example.com",
                Role = "InvalidRole"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.CreateUserAsync(request, "admin"));

            Assert.Contains("Invalid role: InvalidRole", exception.Message);
        }

        [Fact]
        public async Task CreateUserAsync_WithDuplicateUsername_ThrowsInvalidOperationException()
        {
            // Arrange
            var existingUser = IdentityTestHelper.CreateTestUser("existing-id", "existinguser", "existing@example.com");
            var request = new CreateUserRequest
            {
                Username = "existinguser",
                Password = "NewUser@123",
                FullName = "New User",
                Email = "newuser@example.com",
                Role = Roles.Monitor
            };

            _mockUserManager
                .Setup(um => um.FindByNameAsync(request.Username))
                .ReturnsAsync(existingUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.CreateUserAsync(request, "admin"));

            Assert.Contains("Username existinguser already exists", exception.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_WithValidData_UpdatesUser()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com", "Test User");
            var request = new UpdateUserRequest
            {
                FullName = "Updated Name",
                Email = "updated@example.com",
                IsActive = true
            };

            _mockUserManager
                .Setup(um => um.FindByIdAsync("user-id"))
                .ReturnsAsync(user);

            _mockUserManager
                .Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager
                .Setup(um => um.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { Roles.Monitor });

            // Act
            var result = await _userService.UpdateUserAsync("user-id", request, "admin");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Name", result.FullName);
            Assert.Equal("updated@example.com", result.Email);

            _mockUserManager.Verify(um => um.UpdateAsync(It.Is<ApplicationUser>(
                u => u.FullName == "Updated Name" && u.Email == "updated@example.com")), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_WithInvalidUserId_ThrowsKeyNotFoundException()
        {
            // Arrange
            var request = new UpdateUserRequest
            {
                FullName = "Updated Name"
            };

            _mockUserManager
                .Setup(um => um.FindByIdAsync("invalid-id"))
                .ReturnsAsync((ApplicationUser)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _userService.UpdateUserAsync("invalid-id", request, "admin"));

            Assert.Contains("User with ID invalid-id not found", exception.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_WithPasswordChange_UpdatesPassword()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com");
            var request = new UpdateUserRequest
            {
                Password = "NewPassword@123"
            };

            _mockUserManager
                .Setup(um => um.FindByIdAsync("user-id"))
                .ReturnsAsync(user);

            _mockUserManager
                .Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager
                .Setup(um => um.RemovePasswordAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager
                .Setup(um => um.AddPasswordAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager
                .Setup(um => um.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { Roles.Monitor });

            // Act
            var result = await _userService.UpdateUserAsync("user-id", request, "admin");

            // Assert
            _mockUserManager.Verify(um => um.RemovePasswordAsync(It.IsAny<ApplicationUser>()), Times.Once);
            _mockUserManager.Verify(um => um.AddPasswordAsync(It.IsAny<ApplicationUser>(), request.Password), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_WithRootAdmin_ThrowsInvalidOperationException()
        {
            // Arrange
            var adminUser = IdentityTestHelper.CreateTestUser("admin-id", "admin", "admin@example.com");

            _mockUserManager
                .Setup(um => um.FindByIdAsync("admin-id"))
                .ReturnsAsync(adminUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.DeleteUserAsync("admin-id", "someuser"));

            Assert.Contains("Cannot delete root administrator account", exception.Message);
        }

        [Fact]
        public async Task DeleteUserAsync_WithLastActiveAdmin_ThrowsInvalidOperationException()
        {
            // Arrange
            var adminUser = IdentityTestHelper.CreateTestUser("admin-id", "adminuser", "admin@example.com");
            var allUsers = new List<ApplicationUser> { adminUser };

            _mockUserManager
                .Setup(um => um.FindByIdAsync("admin-id"))
                .ReturnsAsync(adminUser);

            _mockUserManager
                .Setup(um => um.GetRolesAsync(adminUser))
                .ReturnsAsync(new List<string> { Roles.Administrator });

            _mockUserManager
                .Setup(um => um.Users)
                .Returns(allUsers.AsQueryable());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.DeleteUserAsync("admin-id", "someuser"));

            Assert.Contains("Cannot delete the last active administrator", exception.Message);
        }

        [Fact]
        public async Task DeleteUserAsync_WithValidUser_DeletesSuccessfully()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com");

            _mockUserManager
                .Setup(um => um.FindByIdAsync("user-id"))
                .ReturnsAsync(user);

            _mockUserManager
                .Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { Roles.Monitor });

            _mockUserManager
                .Setup(um => um.DeleteAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.DeleteUserAsync("user-id", "admin");

            // Assert
            Assert.True(result);
            _mockUserManager.Verify(um => um.DeleteAsync(user), Times.Once);
        }

        [Fact]
        public async Task CanDeleteUserAsync_WithRootAdmin_ReturnsFalse()
        {
            // Arrange
            var adminUser = IdentityTestHelper.CreateTestUser("admin-id", "admin", "admin@example.com");

            _mockUserManager
                .Setup(um => um.FindByIdAsync("admin-id"))
                .ReturnsAsync(adminUser);

            // Act
            var result = await _userService.CanDeleteUserAsync("admin-id", "other-user-id");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanDeleteUserAsync_WithLastActiveAdmin_ReturnsFalse()
        {
            // Arrange
            var adminUser = IdentityTestHelper.CreateTestUser("admin-id", "adminuser", "admin@example.com");
            var allUsers = new List<ApplicationUser> { adminUser };

            _mockUserManager
                .Setup(um => um.FindByIdAsync("admin-id"))
                .ReturnsAsync(adminUser);

            _mockUserManager
                .Setup(um => um.GetRolesAsync(adminUser))
                .ReturnsAsync(new List<string> { Roles.Administrator });

            _mockUserManager
                .Setup(um => um.Users)
                .Returns(allUsers.AsQueryable());

            // Act
            var result = await _userService.CanDeleteUserAsync("admin-id", "other-user-id");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanDeleteUserAsync_WhenDeletingSelf_ReturnsFalse()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com");

            _mockUserManager
                .Setup(um => um.FindByIdAsync("user-id"))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.CanDeleteUserAsync("user-id", "user-id");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanDeleteUserAsync_WithValidUser_ReturnsTrue()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com");
            var adminUser = IdentityTestHelper.CreateTestUser("admin-id", "adminuser", "admin@example.com");
            var allUsers = new List<ApplicationUser> { user, adminUser };

            _mockUserManager
                .Setup(um => um.FindByIdAsync("user-id"))
                .ReturnsAsync(user);

            _mockUserManager
                .Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { Roles.Monitor });

            _mockUserManager
                .Setup(um => um.Users)
                .Returns(allUsers.AsQueryable());

            // Act
            var result = await _userService.CanDeleteUserAsync("user-id", "admin-id");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsPaginatedResults()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                IdentityTestHelper.CreateTestUser("user1", "user1", "user1@example.com"),
                IdentityTestHelper.CreateTestUser("user2", "user2", "user2@example.com"),
                IdentityTestHelper.CreateTestUser("user3", "user3", "user3@example.com")
            };

            _mockUserManager
                .Setup(um => um.Users)
                .Returns(users.AsQueryable());

            foreach (var user in users)
            {
                _mockUserManager
                    .Setup(um => um.GetRolesAsync(user))
                    .ReturnsAsync(new List<string> { Roles.Monitor });
            }

            // Act
            var result = await _userService.GetUsersAsync(1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(1, result.TotalPages);
            Assert.Equal(3, result.Data.Count());
        }

        [Fact]
        public async Task GetUsersAsync_WithPaging_ReturnsCorrectPage()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                IdentityTestHelper.CreateTestUser("user1", "user1", "user1@example.com"),
                IdentityTestHelper.CreateTestUser("user2", "user2", "user2@example.com"),
                IdentityTestHelper.CreateTestUser("user3", "user3", "user3@example.com"),
                IdentityTestHelper.CreateTestUser("user4", "user4", "user4@example.com"),
                IdentityTestHelper.CreateTestUser("user5", "user5", "user5@example.com")
            };

            _mockUserManager
                .Setup(um => um.Users)
                .Returns(users.AsQueryable());

            foreach (var user in users)
            {
                _mockUserManager
                    .Setup(um => um.GetRolesAsync(user))
                    .ReturnsAsync(new List<string> { Roles.Monitor });
            }

            // Act
            var result = await _userService.GetUsersAsync(2, 2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(3, result.TotalPages);
            Assert.Equal(2, result.Page);
            Assert.Equal(2, result.Data.Count());
            var dataList = result.Data.ToList();
            Assert.Equal("user3", dataList[0].Username);
            Assert.Equal("user4", dataList[1].Username);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithValidId_ReturnsUserWithRole()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com", "Test User");

            _mockUserManager
                .Setup(um => um.FindByIdAsync("user-id"))
                .ReturnsAsync(user);

            _mockUserManager
                .Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { Roles.Administrator });

            // Act
            var result = await _userService.GetUserByIdAsync("user-id");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("user-id", result.Id);
            Assert.Equal("testuser", result.Username);
            Assert.Equal("Test User", result.FullName);
            Assert.Equal(Roles.Administrator, result.Role);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithInvalidId_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockUserManager
                .Setup(um => um.FindByIdAsync("invalid-id"))
                .ReturnsAsync((ApplicationUser)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _userService.GetUserByIdAsync("invalid-id"));

            Assert.Contains("User with ID invalid-id not found", exception.Message);
        }

        [Fact]
        public async Task GetActiveUserCountAsync_ReturnsCorrectCount()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                IdentityTestHelper.CreateTestUser("user1", "user1", "user1@example.com", "User 1", true),
                IdentityTestHelper.CreateTestUser("user2", "user2", "user2@example.com", "User 2", true),
                IdentityTestHelper.CreateTestUser("user3", "user3", "user3@example.com", "User 3", false),
                IdentityTestHelper.CreateTestUser("user4", "user4", "user4@example.com", "User 4", true)
            };

            _mockUserManager
                .Setup(um => um.Users)
                .Returns(users.AsQueryable());

            // Act
            var result = await _userService.GetActiveUserCountAsync();

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public async Task IsRootAdminAsync_WithAdminUsername_ReturnsTrue()
        {
            // Arrange
            var adminUser = IdentityTestHelper.CreateTestUser("admin-id", "admin", "admin@example.com");

            _mockUserManager
                .Setup(um => um.FindByIdAsync("admin-id"))
                .ReturnsAsync(adminUser);

            // Act
            var result = await _userService.IsRootAdminAsync("admin-id");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsRootAdminAsync_WithNonAdminUsername_ReturnsFalse()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com");

            _mockUserManager
                .Setup(um => um.FindByIdAsync("user-id"))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.IsRootAdminAsync("user-id");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ChangePasswordAsync_WithValidData_ChangesPassword()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com");
            var newPassword = "NewPassword@123";

            _mockUserManager
                .Setup(um => um.FindByIdAsync("user-id"))
                .ReturnsAsync(user);

            _mockUserManager
                .Setup(um => um.RemovePasswordAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager
                .Setup(um => um.AddPasswordAsync(user, newPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.ChangePasswordAsync("user-id", newPassword, "admin");

            // Assert
            Assert.True(result);
            _mockUserManager.Verify(um => um.RemovePasswordAsync(user), Times.Once);
            _mockUserManager.Verify(um => um.AddPasswordAsync(user, newPassword), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_WithInvalidUserId_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockUserManager
                .Setup(um => um.FindByIdAsync("invalid-id"))
                .ReturnsAsync((ApplicationUser)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _userService.ChangePasswordAsync("invalid-id", "NewPassword@123", "admin"));

            Assert.Contains("User with ID invalid-id not found", exception.Message);
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenRemovePasswordFails_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com");

            _mockUserManager
                .Setup(um => um.FindByIdAsync("user-id"))
                .ReturnsAsync(user);

            _mockUserManager
                .Setup(um => um.RemovePasswordAsync(user))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Failed to remove password" }));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.ChangePasswordAsync("user-id", "NewPassword@123", "admin"));

            Assert.Contains("Failed to remove old password", exception.Message);
        }

        [Theory]
        [InlineData(Roles.Administrator)]
        [InlineData(Roles.Management)]
        [InlineData(Roles.Monitor)]
        public async Task CreateUserAsync_WithDifferentRoles_CreatesUserWithCorrectRole(string role)
        {
            // Arrange
            var request = new CreateUserRequest
            {
                Username = "newuser",
                Password = "NewUser@123",
                FullName = "New User",
                Email = "newuser@example.com",
                Role = role
            };

            _mockUserManager
                .Setup(um => um.FindByNameAsync(request.Username))
                .ReturnsAsync((ApplicationUser)null);

            _mockUserManager
                .Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager
                .Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), request.Role))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.CreateUserAsync(request, "admin");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(role, result.Role);
        }

        [Fact]
        public async Task UpdateUserAsync_WithRoleChange_UpdatesRole()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com");
            var request = new UpdateUserRequest
            {
                Role = Roles.Management
            };

            _mockUserManager
                .Setup(um => um.FindByIdAsync("user-id"))
                .ReturnsAsync(user);

            _mockUserManager
                .Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager
                .Setup(um => um.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { Roles.Monitor });

            _mockUserManager
                .Setup(um => um.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager
                .Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Management))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.UpdateUserAsync("user-id", request, "admin");

            // Assert
            _mockUserManager.Verify(um => um.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Once);
            _mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Management), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_WhenRoleAssignmentFails_DeletesUser()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                Username = "newuser",
                Password = "NewUser@123",
                FullName = "New User",
                Email = "newuser@example.com",
                Role = Roles.Monitor
            };

            ApplicationUser createdUser = null;

            _mockUserManager
                .Setup(um => um.FindByNameAsync(request.Username))
                .ReturnsAsync((ApplicationUser)null);

            _mockUserManager
                .Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .Callback<ApplicationUser, string>((u, p) => createdUser = u)
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager
                .Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), request.Role))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role assignment failed" }));

            _mockUserManager
                .Setup(um => um.DeleteAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.CreateUserAsync(request, "admin"));

            Assert.Contains("Failed to assign role", exception.Message);
            _mockUserManager.Verify(um => um.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Once);
        }
    }
}
