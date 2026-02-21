using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface IPurchaseBatchService
    {
        // Batch CRUD
        Task<List<PurchaseBatch>> GetAllBatchesAsync();
        Task<PaginatedResponse<PurchaseBatch>> GetBatchesPaginatedAsync(int pageNumber, int pageSize, PurchaseBatchSearchDto? filters = null);
        Task<PurchaseBatch?> GetBatchByIdAsync(int id);
        Task<PurchaseBatch> CreateBatchAsync(CreatePurchaseBatchDto batchDto);
        Task<bool> UpdateBatchAsync(int id, UpdatePurchaseBatchDto batchDto);
        Task<bool> DeleteBatchAsync(int id);

        // Batch Items
        Task<List<PurchaseBatchItem>> GetBatchItemsAsync(int batchId);
        Task<PurchaseBatchItem?> GetBatchItemByIdAsync(int itemId);
        Task<PurchaseBatchItem> AddBatchItemAsync(int batchId, CreatePurchaseBatchItemDto itemDto);
        Task<bool> UpdateBatchItemAsync(int itemId, UpdatePurchaseBatchItemDto itemDto);
        Task<bool> DeleteBatchItemAsync(int itemId);

        // Search & Filters
        Task<List<PurchaseBatch>> SearchBatchesAsync(PurchaseBatchSearchDto searchDto);
        Task<List<PurchaseBatchSummary>> GetBatchSummariesAsync();
        Task<List<PurchaseBatch>> GetBatchesBySupplierAsync(int supplierId);
        Task<List<PurchaseBatch>> GetBatchesByStatusAsync(string status);

        // Variant Selection (matching your UI)
        Task<List<VariantForSelectionDto>> GetVariantsForSelectionAsync(string? searchTerm = null);

        // Payment Management
        Task<bool> MakePaymentAsync(BatchPaymentDto paymentDto);
        Task<decimal> GetOutstandingBalanceAsync(int supplierId);

        // Stock Management
        Task<bool> UpdateStockFromBatchAsync(int batchId);
        Task<bool> ReverseStockFromBatchAsync(int batchId);

        // Validation
        Task<bool> BatchNameExistsAsync(string batchName, int? excludeBatchId = null);
        Task<int> GetNextBatchIdAsync();
        Task<int> GetNextItemIdAsync();
    }
}