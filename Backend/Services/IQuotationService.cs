using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface IQuotationService
    {
        // ✅ Create quotation (no PDF, no stock changes)
        Task<Quotation> CreateQuotationAsync(CreateQuotationDto quotationDto);

        // ✅ Generate PDF for existing quotation
        Task<QuotationPdfResponse> GenerateQuotationPdfAsync(int quotationId);

        Task<Quotation?> GetQuotationByIdAsync(int quotationId);
        Task<Quotation?> GetQuotationByNumberAsync(string quotationNumber);
        Task<Quotation?> SearchQuotationAsync(string searchValue);
        Task<List<Quotation>> GetAllQuotationsAsync();
        Task<PaginatedResponse<Quotation>> GetQuotationsPaginatedAsync(int pageNumber, int pageSize, QuotationSearchDto? filters = null);
        Task<List<Quotation>> SearchQuotationsAsync(QuotationSearchDto searchDto);
        Task<List<Quotation>> GetQuotationsByCustomerAsync(int customerId);
        Task<List<Quotation>> GetPendingQuotationsAsync();
    }
}