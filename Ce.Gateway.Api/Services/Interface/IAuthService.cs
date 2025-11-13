using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models.Auth;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services.Interface
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<bool> ValidateTokenAsync(string token);
        Task<ApplicationUser> GetCurrentUserAsync(string userId);
        string GenerateJwtToken(ApplicationUser user, string role);
    }
}
