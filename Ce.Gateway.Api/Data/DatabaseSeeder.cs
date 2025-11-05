using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Services.Auth;
using Microsoft.EntityFrameworkCore;
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
            var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<GatewayDbContext>>();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

            using var context = dbContextFactory.CreateDbContext();

            // Check if any users exist
            if (await context.Users.AnyAsync())
            {
                Log.Information("Users already exist in database. Skipping seed.");
                return;
            }

            // Create default admin user
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = authService.HashPassword("admin123"),
                FullName = "System Administrator",
                Email = "admin@gateway.local",
                Role = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            Log.Information("Default admin user created successfully. Username: admin, Password: admin123");
        }
    }
}
