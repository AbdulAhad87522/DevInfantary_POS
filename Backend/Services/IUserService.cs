using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface IUserService
    {
        // Authentication
        Task<UserLoginResponse?> LoginAsync(LoginDto loginDto);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);

        // CRUD Operations
        Task<List<User>> GetAllUsersAsync(bool includeInactive = false);
        Task<PaginatedResponse<User>> GetUsersPaginatedAsync(int pageNumber, int pageSize, bool includeInactive = false);
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User> CreateUserAsync(CreateUserDto userDto);
        Task<bool> UpdateUserAsync(int id, UpdateUserDto userDto);
        Task<bool> DeleteUserAsync(int id); // Soft delete
        Task<bool> RestoreUserAsync(int id);

        // Search & Filters
        Task<List<User>> SearchUsersAsync(string searchTerm, bool includeInactive = false);
        Task<List<User>> GetUsersByRoleAsync(string role);

        // Status Management
        Task<bool> UpdateUserStatusAsync(int id, bool isActive);
        Task<bool> UpdateLastLoginAsync(int id);

        // Validation
        Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null);
        Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
    }
}