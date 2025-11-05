using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Create roles if they don't exist
            string[] roleNames = { Roles.Administrator, Roles.Management, Roles.Monitor };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    Log.Information($"Role {roleName} created");
                }
            }

            // Check if admin user already exists
            var adminUser = await userManager.FindByNameAsync("admin");
            if (adminUser != null)
            {
                Log.Information("Admin user already exists. Skipping seed.");
                return;
            }

            // Create default admin user
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@gateway.local",
                FullName = "System Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "admin123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Administrator);
                Log.Information("Default admin user created successfully. Username: admin, Password: admin123");
            }
            else
            {
                Log.Error("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
