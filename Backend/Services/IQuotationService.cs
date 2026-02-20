using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface IQuotationService
    {
        Task<List<Quotation>> GetAllQuotationsAsync();
        Task<PaginatedResponse<Quotation>> GetQuotationsPaginatedAsync(int pageNumber, int pageSize, QuotationSearchDto? filters = null);
        Task<Quotation?> GetQuotationByIdAsync(int id);
        Task<Quotation?> GetQuotationByNumberAsync(string quotationNumber);
        Task<Quotation?> SearchQuotationAsync(string searchValue);
        Task<Quotation> CreateQuotationAsync(CreateQuotationDto quotationDto, int staffId = 1);
        Task<List<Quotation>> SearchQuotationsAsync(QuotationSearchDto searchDto);
        Task<List<Quotation>> GetQuotationsByCustomerAsync(int customerId);
        Task<List<Quotation>> GetPendingQuotationsAsync();
        Task<bool> ConvertQuotationToBillAsync(ConvertQuotationToBillDto convertDto, int staffId = 1);
    }
}