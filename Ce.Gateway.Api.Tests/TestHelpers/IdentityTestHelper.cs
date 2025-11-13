using Ce.Gateway.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Tests.TestHelpers
{
    /// <summary>
    /// Helper class for creating mock Identity managers and test data
    /// </summary>
    public static class IdentityTestHelper
    {
        /// <summary>
        /// Creates a mock UserManager with basic setup
        /// </summary>
        public static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var options = new Mock<IOptions<IdentityOptions>>();
            var passwordHasher = new Mock<IPasswordHasher<ApplicationUser>>();
            var userValidators = new List<IUserValidator<ApplicationUser>>();
            var passwordValidators = new List<IPasswordValidator<ApplicationUser>>();
            var keyNormalizer = new Mock<ILookupNormalizer>();
            var errors = new Mock<IdentityErrorDescriber>();
            var services = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<UserManager<ApplicationUser>>>();

            var userManager = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                options.Object,
                passwordHasher.Object,
                userValidators,
                passwordValidators,
                keyNormalizer.Object,
                errors.Object,
                services.Object,
                logger.Object);

            return userManager;
        }

        /// <summary>
        /// Creates a mock RoleManager with basic setup
        /// </summary>
        public static Mock<RoleManager<IdentityRole>> CreateMockRoleManager()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            var roleValidators = new List<IRoleValidator<IdentityRole>>();
            var keyNormalizer = new Mock<ILookupNormalizer>();
            var errors = new Mock<IdentityErrorDescriber>();
            var logger = new Mock<ILogger<RoleManager<IdentityRole>>>();

            var roleManager = new Mock<RoleManager<IdentityRole>>(
                store.Object,
                roleValidators,
                keyNormalizer.Object,
                errors.Object,
                logger.Object);

            return roleManager;
        }

        /// <summary>
        /// Creates a mock SignInManager with basic setup
        /// </summary>
        public static Mock<SignInManager<ApplicationUser>> CreateMockSignInManager(
            UserManager<ApplicationUser> userManager = null)
        {
            var mockUserManager = userManager != null
                ? Mock.Get(userManager)
                : CreateMockUserManager();

            var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var options = new Mock<IOptions<IdentityOptions>>();
            var logger = new Mock<ILogger<SignInManager<ApplicationUser>>>();
            var schemes = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
            var confirmation = new Mock<IUserConfirmation<ApplicationUser>>();

            var signInManager = new Mock<SignInManager<ApplicationUser>>(
                mockUserManager.Object,
                contextAccessor.Object,
                claimsFactory.Object,
                options.Object,
                logger.Object,
                schemes.Object,
                confirmation.Object);

            return signInManager;
        }

        /// <summary>
        /// Creates test configuration with JWT settings
        /// </summary>
        public static IConfiguration CreateTestConfiguration()
        {
            var configurationData = new Dictionary<string, string>
            {
                { "Tokens:Key", "ThisIsAVerySecretKeyForTestingPurposesOnly12345" },
                { "Tokens:Issuer", "TestIssuer" },
                { "Tokens:Audience", "TestAudience" }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();
        }

        /// <summary>
        /// Creates a test user with specified properties
        /// </summary>
        public static ApplicationUser CreateTestUser(
            string id = null,
            string username = "testuser",
            string email = "testuser@example.com",
            string fullName = "Test User",
            bool isActive = true)
        {
            return new ApplicationUser
            {
                Id = id ?? Guid.NewGuid().ToString(),
                UserName = username,
                Email = email,
                FullName = fullName,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow,
                NormalizedUserName = username.ToUpper(),
                NormalizedEmail = email?.ToUpper()
            };
        }

        /// <summary>
        /// Creates multiple test users with different roles
        /// </summary>
        public static List<ApplicationUser> CreateTestUsers()
        {
            return new List<ApplicationUser>
            {
                CreateTestUser("admin-id", "admin", "admin@example.com", "Administrator", true),
                CreateTestUser("mgmt-id", "manager", "manager@example.com", "Manager User", true),
                CreateTestUser("monitor-id", "monitor", "monitor@example.com", "Monitor User", true),
                CreateTestUser("inactive-id", "inactive", "inactive@example.com", "Inactive User", false)
            };
        }

        /// <summary>
        /// Hashes a password using Identity's default password hasher
        /// </summary>
        public static string HashPassword(ApplicationUser user, string password)
        {
            var hasher = new PasswordHasher<ApplicationUser>();
            return hasher.HashPassword(user, password);
        }

        /// <summary>
        /// Sets up a UserManager mock with a predefined set of users
        /// </summary>
        public static void SetupUserManagerWithUsers(
            Mock<UserManager<ApplicationUser>> mockUserManager,
            List<ApplicationUser> users)
        {
            foreach (var user in users)
            {
                mockUserManager
                    .Setup(um => um.FindByIdAsync(user.Id))
                    .ReturnsAsync(user);

                mockUserManager
                    .Setup(um => um.FindByNameAsync(user.UserName))
                    .ReturnsAsync(user);
            }
        }

        /// <summary>
        /// Sets up a UserManager mock with user-role mappings
        /// </summary>
        public static void SetupUserManagerWithRoles(
            Mock<UserManager<ApplicationUser>> mockUserManager,
            Dictionary<string, List<string>> userRoleMappings)
        {
            foreach (var mapping in userRoleMappings)
            {
                var userId = mapping.Key;
                var roles = mapping.Value;

                mockUserManager
                    .Setup(um => um.GetRolesAsync(It.Is<ApplicationUser>(u => u.Id == userId)))
                    .ReturnsAsync(roles);
            }
        }
    }
}
