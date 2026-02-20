using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface IProductService
    {
        // Product CRUD
        Task<List<Product>> GetAllProductsAsync(bool includeInactive = false);
        Task<PaginatedResponse<Product>> GetProductsPaginatedAsync(int pageNumber, int pageSize, bool includeInactive = false);
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product> CreateProductAsync(CreateProductDto productDto);
        Task<bool> UpdateProductAsync(int id, UpdateProductDto productDto);
        Task<bool> DeleteProductAsync(int id); // Soft delete
        Task<bool> RestoreProductAsync(int id);

        // Product Variants
        Task<List<ProductVariant>> GetProductVariantsAsync(int productId);
        Task<ProductVariant?> GetVariantByIdAsync(int variantId);
        Task<ProductVariant> AddVariantAsync(int productId, CreateProductVariantDto variantDto);
        Task<bool> UpdateVariantAsync(int variantId, UpdateProductVariantDto variantDto);
        Task<bool> DeleteVariantAsync(int variantId); // Soft delete
        Task<bool> UpdateVariantStockAsync(int variantId, decimal quantityChange, string reason);

        // Search & Filters
        Task<PaginatedResponse<Product>> SearchProductsAsync(ProductSearchDto searchDto);
        Task<List<Product>> GetProductsByCategoryAsync(int categoryId, bool includeInactive = false);
        Task<List<Product>> GetProductsBySupplierAsync(int supplierId, bool includeInactive = false);
        Task<List<ProductWithStock>> GetProductsWithStockSummaryAsync();

        // Stock Management
        Task<List<ProductVariant>> GetLowStockItemsAsync(decimal threshold = 10);
        Task<decimal> GetTotalInventoryValueAsync();
        Task<List<ProductVariant>> GetOutOfStockItemsAsync();

        // Categories (from lookup)
        Task<List<CategoryDto>> GetAllCategoriesAsync();
        Task<CategoryDto?> GetCategoryByIdAsync(int categoryId);

        // Validation
        Task<bool> ProductNameExistsAsync(string name, int? excludeProductId = null);
        Task<bool> VariantCombinationExistsAsync(int productId, string? size, string? color, string? classType, int? excludeVariantId = null);
    }
}