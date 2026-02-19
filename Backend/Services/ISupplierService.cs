using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface ISupplierService
    {
        Task<List<Supplier>> GetAllSuppliersAsync(bool includeInactive = false);
        Task<PaginatedResponse<Supplier>> GetSuppliersPaginatedAsync(int pageNumber, int pageSize, bool includeInactive = false);
        Task<Supplier?> GetSupplierByIdAsync(int id);
        Task<Supplier> CreateSupplierAsync(SupplierDto supplierDto);
        Task<bool> UpdateSupplierAsync(int id, SupplierUpdateDto supplierDto);
        Task<bool> DeleteSupplierAsync(int id);
        Task<bool> RestoreSupplierAsync(int id);
        Task<List<Supplier>> SearchSuppliersAsync(string searchTerm, bool includeInactive = false);
        Task<decimal> GetSupplierBalanceAsync(int id);
        Task<bool> UpdateSupplierBalanceAsync(int id, decimal amount);
        Task<List<Supplier>> GetSuppliersWithBalanceAsync();
    }
}   