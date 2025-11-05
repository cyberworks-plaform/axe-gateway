using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Models.Auth;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services.Auth
{
    public class UserService : IUserService
    {
        private readonly IDbContextFactory<GatewayDbContext> _dbContextFactory;
        private readonly IAuthService _authService;

        public UserService(IDbContextFactory<GatewayDbContext> dbContextFactory, IAuthService authService)
        {
            _dbContextFactory = dbContextFactory;
            _authService = authService;
        }

        public async Task<PaginatedResult<UserDto>> GetUsersAsync(int page, int pageSize)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var query = context.Users.OrderByDescending(u => u.CreatedAt);
            var total = await query.CountAsync();
            
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = users.Select(MapToDto).ToList();

            return new PaginatedResult<UserDto>
            {
                Data = userDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var user = await context.Users.FindAsync(userId);
            if (user == null) return null;

            return MapToDto(user);
        }

        public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            // Check if username already exists
            var existingUser = await context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);
            
            if (existingUser != null)
            {
                throw new InvalidOperationException("Username already exists");
            }

            // Validate role
            if (!IsValidRole(request.Role))
            {
                throw new InvalidOperationException("Invalid role. Must be Administrator, Management, or Monitor");
            }

            var user = new User
            {
                Username = request.Username,
                PasswordHash = _authService.HashPassword(request.Password),
                FullName = request.FullName,
                Email = request.Email,
                Role = request.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return MapToDto(user);
        }

        public async Task<UserDto> UpdateUserAsync(int userId, UpdateUserRequest request)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var user = await context.Users.FindAsync(userId);
            if (user == null) return null;

            if (!string.IsNullOrEmpty(request.FullName))
                user.FullName = request.FullName;

            if (!string.IsNullOrEmpty(request.Email))
                user.Email = request.Email;

            if (!string.IsNullOrEmpty(request.Role))
            {
                if (!IsValidRole(request.Role))
                {
                    throw new InvalidOperationException("Invalid role. Must be Administrator, Management, or Monitor");
                }
                user.Role = request.Role;
            }

            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive.Value;

            if (!string.IsNullOrEmpty(request.Password))
                user.PasswordHash = _authService.HashPassword(request.Password);

            user.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return MapToDto(user);
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var user = await context.Users.FindAsync(userId);
            if (user == null) return false;

            context.Users.Remove(user);
            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var user = await context.Users.FindAsync(userId);
            if (user == null) return false;

            user.PasswordHash = _authService.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return true;
        }

        private bool IsValidRole(string role)
        {
            return role == "Administrator" || role == "Management" || role == "Monitor";
        }

        private UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}
