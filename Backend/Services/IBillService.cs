using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface IBillService
    {
        Task<List<Bill>> GetAllBillsAsync();
        Task<PaginatedResponse<Bill>> GetBillsPaginatedAsync(int pageNumber, int pageSize, BillSearchDto? filters = null);
        Task<Bill?> GetBillByIdAsync(int id);
        Task<Bill?> GetBillByNumberAsync(string billNumber);

        // ✅ ONLY ONE CreateBillAsync - returns BillWithPdfResponse
        Task<BillWithPdfResponse> CreateBillAsync(CreateBillDto billDto, int staffId = 1);

        Task<List<Bill>> SearchBillsAsync(BillSearchDto searchDto);
        Task<List<Bill>> GetBillsByCustomerAsync(int customerId);
        Task<List<Bill>> GetPendingBillsAsync();
        Task<decimal> GetCustomerOutstandingBalanceAsync(int customerId);
    }
}