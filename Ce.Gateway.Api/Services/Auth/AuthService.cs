using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IDbContextFactory<GatewayDbContext> _dbContextFactory;
        private readonly IConfiguration _configuration;

        public AuthService(IDbContextFactory<GatewayDbContext> dbContextFactory, IConfiguration configuration)
        {
            _dbContextFactory = dbContextFactory;
            _configuration = configuration;
        }

        public async Task<LoginResponse> AuthenticateAsync(string username, string password)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null || !VerifyPassword(password, user.PasswordHash))
            {
                return null;
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            var token = GenerateJwtToken(user.Id, user.Username, user.Role);

            return new LoginResponse
            {
                Token = token,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                Email = user.Email
            };
        }

        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var user = await context.Users.FindAsync(userId);
            if (user == null) return null;

            return MapToDto(user);
        }

        public async Task<UserDto> GetUserByUsernameAsync(string username)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
            
            if (user == null) return null;

            return MapToDto(user);
        }

        public string GenerateJwtToken(int userId, string username, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Tokens:Key"]);
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, role)
                }),
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

        public string HashPassword(string password)
        {
            using var hmac = new HMACSHA512();
            var salt = hmac.Key;
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            
            var hashBytes = new byte[salt.Length + hash.Length];
            Buffer.BlockCopy(salt, 0, hashBytes, 0, salt.Length);
            Buffer.BlockCopy(hash, 0, hashBytes, salt.Length, hash.Length);
            
            return Convert.ToBase64String(hashBytes);
        }

        private const int SaltLength = 128;

        public bool VerifyPassword(string password, string passwordHash)
        {
            var hashBytes = Convert.FromBase64String(passwordHash);
            var salt = new byte[SaltLength];
            Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltLength);
            
            using var hmac = new HMACSHA512(salt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            
            for (int i = 0; i < computedHash.Length; i++)
            {
                if (hashBytes[i + SaltLength] != computedHash[i])
                    return false;
            }
            
            return true;
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
