using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HardwareStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        #region Product Endpoints

        /// <summary>
        /// Get all products
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<Product>>>> GetAll([FromQuery] bool includeInactive = false)
        {
            try
            {
                var products = await _productService.GetAllProductsAsync(includeInactive);
                return Ok(ApiResponse<List<Product>>.SuccessResponse(products, $"Retrieved {products.Count} products"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll");
                // Return actual error details
                return StatusCode(500, ApiResponse<List<Product>>.ErrorResponse(
                    "Internal server error",
                    new List<string> {
                ex.Message,
                ex.InnerException?.Message ?? "",
                ex.StackTrace ?? ""
                    }));
            }
        }

        /// <summary>
        /// Get paginated products
        /// </summary>
        [HttpGet("paginated")]
        public async Task<ActionResult<PaginatedResponse<Product>>> GetPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                var result = await _productService.GetProductsPaginatedAsync(pageNumber, pageSize, includeInactive);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaginated");
                return StatusCode(500, new PaginatedResponse<Product> { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get product by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Product>>> GetById(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                    return NotFound(ApiResponse<Product>.ErrorResponse($"Product with ID {id} not found"));

                return Ok(ApiResponse<Product>.SuccessResponse(product));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetById for ID {id}");
                return StatusCode(500, ApiResponse<Product>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Create a new product
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<Product>>> Create([FromBody] CreateProductDto productDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<Product>.ErrorResponse("Validation failed", errors));
                }

                var product = await _productService.CreateProductAsync(productDto);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = product.ProductId },
                    ApiResponse<Product>.SuccessResponse(product, "Product created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<Product>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return StatusCode(500, ApiResponse<Product>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Update an existing product
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] UpdateProductDto productDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Validation failed", errors));
                }

                var success = await _productService.UpdateProductAsync(id, productDto);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Product with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Product updated successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Update for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Delete (soft delete) a product
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            try
            {
                var success = await _productService.DeleteProductAsync(id);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Product with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Product deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Delete for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Restore a deleted product
        /// </summary>
        [HttpPost("{id}/restore")]
        public async Task<ActionResult<ApiResponse<bool>>> Restore(int id)
        {
            try
            {
                var success = await _productService.RestoreProductAsync(id);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Product with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Product restored successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Restore for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        #endregion

        #region Variant Endpoints

        /// <summary>
        /// Get all variants for a specific product
        /// </summary>
        [HttpGet("{productId}/variants")]
        public async Task<ActionResult<ApiResponse<List<ProductVariant>>>> GetProductVariants(int productId)
        {
            try
            {
                var variants = await _productService.GetProductVariantsAsync(productId);
                return Ok(ApiResponse<List<ProductVariant>>.SuccessResponse(variants, $"Found {variants.Count} variants"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting variants for product {productId}");
                return StatusCode(500, ApiResponse<List<ProductVariant>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get variant by ID
        /// </summary>
        [HttpGet("variants/{variantId}")]
        public async Task<ActionResult<ApiResponse<ProductVariant>>> GetVariantById(int variantId)
        {
            try
            {
                var variant = await _productService.GetVariantByIdAsync(variantId);
                if (variant == null)
                    return NotFound(ApiResponse<ProductVariant>.ErrorResponse($"Variant with ID {variantId} not found"));

                return Ok(ApiResponse<ProductVariant>.SuccessResponse(variant));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting variant {variantId}");
                return StatusCode(500, ApiResponse<ProductVariant>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Add variant to a product
        /// </summary>
        [HttpPost("{productId}/variants")]
        public async Task<ActionResult<ApiResponse<ProductVariant>>> AddVariant(int productId, [FromBody] CreateProductVariantDto variantDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<ProductVariant>.ErrorResponse("Validation failed", errors));
                }

                var variant = await _productService.AddVariantAsync(productId, variantDto);

                return CreatedAtAction(
                    nameof(GetVariantById),
                    new { variantId = variant.VariantId },
                    ApiResponse<ProductVariant>.SuccessResponse(variant, "Variant added successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<ProductVariant>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding variant to product {productId}");
                return StatusCode(500, ApiResponse<ProductVariant>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Update variant
        /// </summary>
        [HttpPut("variants/{variantId}")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateVariant(int variantId, [FromBody] UpdateProductVariantDto variantDto)
        {
            try
            {
                if (variantId != variantDto.VariantId)
                {
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Variant ID mismatch"));
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Validation failed", errors));
                }

                var success = await _productService.UpdateVariantAsync(variantId, variantDto);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Variant with ID {variantId} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Variant updated successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating variant {variantId}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Delete variant
        /// </summary>
        [HttpDelete("variants/{variantId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteVariant(int variantId)
        {
            try
            {
                var success = await _productService.DeleteVariantAsync(variantId);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Variant with ID {variantId} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Variant deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting variant {variantId}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Update variant stock
        /// </summary>
        [HttpPatch("variants/{variantId}/stock")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateStock(
            int variantId,
            [FromBody] decimal quantityChange,
            [FromQuery] string reason = "Manual adjustment")
        {
            try
            {
                var success = await _productService.UpdateVariantStockAsync(variantId, quantityChange, reason);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Variant with ID {variantId} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Stock updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating stock for variant {variantId}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        #endregion

        #region Search & Reports

        /// <summary>
        /// Advanced product search
        /// </summary>
        [HttpPost("search")]
        public async Task<ActionResult<PaginatedResponse<Product>>> Search([FromBody] ProductSearchDto searchDto)
        {
            try
            {
                var result = await _productService.SearchProductsAsync(searchDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Search");
                return StatusCode(500, new PaginatedResponse<Product> { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get products by category
        /// </summary>
        [HttpGet("by-category/{categoryId}")]
        public async Task<ActionResult<ApiResponse<List<Product>>>> GetByCategory(int categoryId, [FromQuery] bool includeInactive = false)
        {
            try
            {
                var products = await _productService.GetProductsByCategoryAsync(categoryId, includeInactive);
                return Ok(ApiResponse<List<Product>>.SuccessResponse(products, $"Found {products.Count} products"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting products for category {categoryId}");
                return StatusCode(500, ApiResponse<List<Product>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get products by supplier
        /// </summary>
        [HttpGet("by-supplier/{supplierId}")]
        public async Task<ActionResult<ApiResponse<List<Product>>>> GetBySupplier(int supplierId, [FromQuery] bool includeInactive = false)
        {
            try
            {
                var products = await _productService.GetProductsBySupplierAsync(supplierId, includeInactive);
                return Ok(ApiResponse<List<Product>>.SuccessResponse(products, $"Found {products.Count} products"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting products for supplier {supplierId}");
                return StatusCode(500, ApiResponse<List<Product>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get products with stock summary
        /// </summary>
        [HttpGet("stock-summary")]
        public async Task<ActionResult<ApiResponse<List<ProductWithStock>>>> GetStockSummary()
        {
            try
            {
                var products = await _productService.GetProductsWithStockSummaryAsync();
                return Ok(ApiResponse<List<ProductWithStock>>.SuccessResponse(products));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock summary");
                return StatusCode(500, ApiResponse<List<ProductWithStock>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get low stock items
        /// </summary>
        [HttpGet("low-stock")]
        public async Task<ActionResult<ApiResponse<List<ProductVariant>>>> GetLowStock([FromQuery] decimal threshold = 10)
        {
            try
            {
                var items = await _productService.GetLowStockItemsAsync(threshold);
                return Ok(ApiResponse<List<ProductVariant>>.SuccessResponse(items, $"Found {items.Count} low stock items"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock items");
                return StatusCode(500, ApiResponse<List<ProductVariant>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get out of stock items
        /// </summary>
        [HttpGet("out-of-stock")]
        public async Task<ActionResult<ApiResponse<List<ProductVariant>>>> GetOutOfStock()
        {
            try
            {
                var items = await _productService.GetOutOfStockItemsAsync();
                return Ok(ApiResponse<List<ProductVariant>>.SuccessResponse(items, $"Found {items.Count} out of stock items"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting out of stock items");
                return StatusCode(500, ApiResponse<List<ProductVariant>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get total inventory value
        /// </summary>
        [HttpGet("inventory-value")]
        public async Task<ActionResult<ApiResponse<decimal>>> GetInventoryValue()
        {
            try
            {
                var value = await _productService.GetTotalInventoryValueAsync();
                return Ok(ApiResponse<decimal>.SuccessResponse(value, $"Total inventory value: {value:C}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory value");
                return StatusCode(500, ApiResponse<decimal>.ErrorResponse("Internal server error"));
            }
        }

        #endregion

        #region Categories

        /// <summary>
        /// Get all product categories (from lookup)
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
        {
            try
            {
                var categories = await _productService.GetAllCategoriesAsync();
                return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(categories, $"Found {categories.Count} categories"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, ApiResponse<List<CategoryDto>>.ErrorResponse("Internal server error"));
            }
        }

        #endregion
    }
}