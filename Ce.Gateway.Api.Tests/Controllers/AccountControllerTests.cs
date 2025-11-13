using Ce.Gateway.Api.Controllers.Pages;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models.Auth;
using Ce.Gateway.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using IdentityResult = Microsoft.AspNetCore.Identity.IdentityResult;
using IdentityError = Microsoft.AspNetCore.Identity.IdentityError;
using UserManager = Microsoft.AspNetCore.Identity.UserManager<Ce.Gateway.Api.Entities.ApplicationUser>;
using SignInManager = Microsoft.AspNetCore.Identity.SignInManager<Ce.Gateway.Api.Entities.ApplicationUser>;

namespace Ce.Gateway.Api.Tests.Controllers
{
    /// <summary>
    /// Unit tests for AccountController (MVC)
    /// </summary>
    public class AccountControllerTests
    {
        private readonly Mock<SignInManager> _mockSignInManager;
        private readonly Mock<UserManager> _mockUserManager;
        private readonly Mock<ILogger<AccountController>> _mockLogger;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            _mockUserManager = IdentityTestHelper.CreateMockUserManager();
            _mockSignInManager = IdentityTestHelper.CreateMockSignInManager(_mockUserManager.Object);
            _mockLogger = new Mock<ILogger<AccountController>>();

            _controller = new AccountController(
                _mockSignInManager.Object,
                _mockUserManager.Object,
                _mockLogger.Object);

            // Setup TempData
            _controller.TempData = new TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public void Login_Get_ReturnsView()
        {
            // Act
            var result = _controller.Login();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public async Task Login_Post_ValidCredentials_RedirectsToDashboard()
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
                .Setup(sm => sm.PasswordSignInAsync(request.Username, request.Password, false, true))
                .ReturnsAsync(SignInResult.Success);

            _mockUserManager
                .Setup(um => um.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Dashboard", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Login_Post_InvalidCredentials_ReturnsViewWithError()
        {
            // Arrange
            var request = new LoginRequest
            {
                Username = "testuser",
                Password = "WrongPassword"
            };

            _mockUserManager
                .Setup(um => um.FindByNameAsync(request.Username))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains("Invalid login attempt", _controller.ModelState.Root.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Login_Post_InactiveUser_ReturnsViewWithError()
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

            // Act
            var result = await _controller.Login(request);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Logout_Post_RedirectsToLogin()
        {
            // Arrange
            _mockSignInManager
                .Setup(sm => sm.SignOutAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Logout();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public async Task Profile_ReturnsViewWithUserData()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com", "Test User", true);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "user-id"),
                        new Claim(ClaimTypes.Name, "testuser")
                    }, "TestAuth"))
                }
            };

            _mockUserManager
                .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            _mockUserManager
                .Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { Roles.Monitor });

            // Act
            var result = await _controller.Profile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(user, viewResult.Model);
            Assert.Equal(Roles.Monitor, _controller.ViewBag.Role);
        }

        [Fact]
        public void ChangePassword_Get_ReturnsView()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "user-id")
                    }, "TestAuth"))
                }
            };

            // Act
            var result = _controller.ChangePassword();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public async Task ChangePassword_WithValidData_RedirectsToProfile()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com", "Test User", true);
            var model = new ChangePasswordCurrentUserRequest
            {
                CurrentPassword = "OldPass@123",
                NewPassword = "NewPass@123",
                ConfirmPassword = "NewPass@123"
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "user-id")
                    }, "TestAuth"))
                }
            };

            _mockUserManager
                .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            _mockUserManager
                .Setup(um => um.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            _mockSignInManager
                .Setup(sm => sm.RefreshSignInAsync(user))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ChangePassword(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Profile", redirectResult.ActionName);
            Assert.Equal("Password changed successfully", _controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task ChangePassword_WithInvalidCurrentPassword_ReturnsViewWithError()
        {
            // Arrange
            var user = IdentityTestHelper.CreateTestUser("user-id", "testuser", "test@example.com", "Test User", true);
            var model = new ChangePasswordCurrentUserRequest
            {
                CurrentPassword = "WrongPassword",
                NewPassword = "NewPass@123",
                ConfirmPassword = "NewPass@123"
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "user-id")
                    }, "TestAuth"))
                }
            };

            _mockUserManager
                .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            _mockUserManager
                .Setup(um => um.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError
                {
                    Description = "Incorrect password"
                }));

            // Act
            var result = await _controller.ChangePassword(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
        }
    }
}
