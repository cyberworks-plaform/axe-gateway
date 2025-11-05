using Ce.Gateway.Api.Models.Auth;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services.Auth
{
    public interface IAuthService
    {
        Task<LoginResponse> AuthenticateAsync(string username, string password);
        Task<UserDto> GetUserByIdAsync(int userId);
        Task<UserDto> GetUserByUsernameAsync(string username);
        string GenerateJwtToken(int userId, string username, string role);
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
    }
}
