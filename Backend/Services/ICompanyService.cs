using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface ICompanyService
    {
        Task<List<Company>> GetAllAsync(bool includeInactive = false);
        Task<Company?> GetByIdAsync(int id);
        Task<Company> CreateAsync(CompanyDto dto);
        Task<bool> UpdateAsync(int id, CompanyUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> RestoreAsync(int id);
    }
}
