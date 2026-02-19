using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetAllCustomersAsync(bool includeInactive = false);
        Task<PaginatedResponse<Customer>> GetCustomersPaginatedAsync(int pageNumber, int pageSize, bool includeInactive = false);
        Task<Customer?> GetCustomerByIdAsync(int id);
        Task<Customer> CreateCustomerAsync(CustomerDto customerDto);
        Task<bool> UpdateCustomerAsync(int id, CustomerUpdateDto customerDto);
        Task<bool> DeleteCustomerAsync(int id);
        Task<bool> RestoreCustomerAsync(int id);
        Task<List<Customer>> SearchCustomersAsync(string searchTerm, bool includeInactive = false);
        Task<decimal> GetCustomerBalanceAsync(int id);
        Task<bool> UpdateCustomerBalanceAsync(int id, decimal amount);
    }
}