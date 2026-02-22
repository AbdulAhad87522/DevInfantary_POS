using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface ISupplierBillService
    {
        Task<List<SupplierBillSummary>> GetAllSupplierBillSummariesAsync(string? search = null);
        Task<SupplierBillSummary?> GetSupplierBillSummaryAsync(int supplierId);
        Task<List<SupplierBatchDetail>> GetSupplierBatchesAsync(int supplierId);
        Task<SupplierBatchDetail?> GetBatchDetailAsync(int batchId);
        Task<List<SupplierPaymentRecord>> GetSupplierPaymentRecordsAsync(int supplierId);
        Task<List<SupplierPaymentRecord>> GetBatchPaymentRecordsAsync(int batchId);
        Task<PaymentDistributionResult> AddSupplierPaymentAsync(AddSupplierPaymentDto paymentDto);
    }
}