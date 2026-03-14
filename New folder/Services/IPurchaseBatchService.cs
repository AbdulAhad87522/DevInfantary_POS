using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface IPurchaseBatchService
    {
        Task<List<PurchaseBatch>> GetAllBatchesAsync();
        Task<PaginatedResponse<PurchaseBatch>> GetBatchesPaginatedAsync(int pageNumber, int pageSize, PurchaseBatchSearchDto? filters = null);
        Task<PurchaseBatch?> GetBatchByIdAsync(int id);
        Task<PurchaseBatch> CreateBatchAsync(CreatePurchaseBatchDto batchDto);
        Task<bool> UpdateBatchAsync(int id, UpdatePurchaseBatchDto batchDto);
        Task<bool> DeleteBatchAsync(int id);
        Task<List<PurchaseBatch>> SearchBatchesAsync(PurchaseBatchSearchDto searchDto);
        Task<List<PurchaseBatch>> GetBatchesBySupplierAsync(int supplierId);
        Task<List<PurchaseBatch>> GetPendingBatchesAsync();
        Task<List<ProductVariantForBatch>> GetProductVariantsForBatchAsync(string? searchTerm = null);
        Task<int> GetNextBatchIdAsync();
    }
}