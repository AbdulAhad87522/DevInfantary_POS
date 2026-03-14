using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface IReturnService
    {
        Task<List<Return>> GetAllReturnsAsync();
        Task<PaginatedResponse<Return>> GetReturnsPaginatedAsync(int pageNumber, int pageSize, ReturnSearchDto? filters = null);
        Task<Return?> GetReturnByIdAsync(int id);
        Task<Return> ProcessReturnAsync(ProcessReturnDto returnDto);
        Task<List<Return>> SearchReturnsAsync(ReturnSearchDto searchDto);
        Task<List<Return>> GetReturnsByCustomerAsync(int customerId);
        Task<List<Return>> GetReturnsByBillAsync(int billId);
        Task<BillForReturnDto?> GetBillForReturnAsync(string billNumber);
    }
}