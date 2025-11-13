using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Models.Auth;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services.Interface
{
    public interface IUserService
    {
        /// <summary>
        /// Get paginated list of users with optimized query (no N+1)
        /// </summary>
        Task<PaginatedResult<UserDto>> GetUsersAsync(int page, int pageSize);
        
        /// <summary>
        /// Get all users without pagination - optimized for display (no N+1)
        /// </summary>
        Task<System.Collections.Generic.List<UserDto>> GetAllUsersAsync();
        
        Task<UserDto> GetUserByIdAsync(string id);
        Task<UserDto> CreateUserAsync(CreateUserRequest request, string createdBy);
        Task<UserDto> UpdateUserAsync(string id, UpdateUserRequest request, string updatedBy);
        Task<bool> DeleteUserAsync(string id, string deletedBy, string currentUserId = null);
        Task<bool> CanDeleteUserAsync(string id, string currentUserId);
        Task<int> GetActiveUserCountAsync();
        Task<bool> IsRootAdminAsync(string userId);
        Task<bool> ChangePasswordAsync(string id, string newPassword, string changedBy);
        Task<bool> UnlockUserAsync(string id, string unlockedBy);
    }
}
