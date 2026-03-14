// Services/IStaffService.cs
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface IStaffService
    {
        Task<List<Staff>> GetAllStaffAsync(bool includeInactive = false);
        Task<PaginatedResponse<Staff>> GetStaffPaginatedAsync(int pageNumber, int pageSize, bool includeInactive = false);
        Task<Staff?> GetStaffByIdAsync(int id);
        Task<Staff> CreateStaffAsync(StaffDto staffDto);
        Task<bool> UpdateStaffAsync(int id, StaffUpdateDto staffDto);
        Task<bool> DeleteStaffAsync(int id);
        Task<bool> RestoreStaffAsync(int id);
        Task<List<Staff>> SearchStaffAsync(string searchTerm, bool includeInactive = false);
        Task<bool> ChangePasswordAsync(int id, StaffChangePasswordDto dto);
        Task<List<Staff>> GetStaffByRoleAsync(string roleName);
    }
}