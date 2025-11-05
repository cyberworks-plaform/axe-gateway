using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Models.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services.Auth
{
    public interface IUserService
    {
        Task<PaginatedResult<UserDto>> GetUsersAsync(int page, int pageSize);
        Task<UserDto> GetUserByIdAsync(int userId);
        Task<UserDto> CreateUserAsync(CreateUserRequest request);
        Task<UserDto> UpdateUserAsync(int userId, UpdateUserRequest request);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> ChangePasswordAsync(int userId, string newPassword);
    }
}
