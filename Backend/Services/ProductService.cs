using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;

namespace HardwareStoreAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ILogger<ProductService> logger)
        {
            _db = DatabaseHelper.Instance;
            _logger = logger;
        }

        #region Product CRUD

        public async Task<List<Product>> GetAllProductsAsync(bool includeInactive = false)
        {
            var products = new List<Product>();
            string query = @"
                SELECT p.*, l.value as category_name, s.name as supplier_name
                FROM products p
                LEFT JOIN lookup l ON p.category_id = l.lookup_id AND l.type = 'category'
                LEFT JOIN supplier s ON p.supplier_id = s.supplier_id
                " + (includeInactive ? "" : "WHERE p.is_active = 1") + @"
                ORDER BY p.name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    products.Add(MapToProduct(reader));
                }

                _logger.LogInformation($"Retrieved {products.Count} products");
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all products");
                throw;
            }
        }

        public async Task<PaginatedResponse<Product>> GetProductsPaginatedAsync(int pageNumber, int pageSize, bool includeInactive = false)
        {
            var response = new PaginatedResponse<Product>
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            try
            {
                string whereClause = includeInactive ? "" : "WHERE p.is_active = 1";

                // Get total count
                string countQuery = $"SELECT COUNT(*) FROM products p {whereClause}";
                response.TotalRecords = Convert.ToInt32(await _db.ExecuteScalarAsync(countQuery));

                // Get paginated data
                int offset = (pageNumber - 1) * pageSize;
                string query = $@"
                    SELECT p.*, l.value as category_name, s.name as supplier_name
                    FROM products p
                    LEFT JOIN lookup l ON p.category_id = l.lookup_id AND l.type = 'category'
                    LEFT JOIN supplier s ON p.supplier_id = s.supplier_id
                    {whereClause}
                    ORDER BY p.name 
                    LIMIT @pageSize OFFSET @offset";

                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@pageSize", pageSize);
                command.Parameters.AddWithValue("@offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    response.Data.Add(MapToProduct(reader));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated products");
                throw;
            }
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            string query = @"
                SELECT p.*, l.value as category_name, s.name as supplier_name
                FROM products p
                LEFT JOIN lookup l ON p.category_id = l.lookup_id AND l.type = 'category'
                LEFT JOIN supplier s ON p.supplier_id = s.supplier_id
                WHERE p.product_id = @id";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var product = MapToProduct(reader);
                    reader.Close();

                    // Get variants for this product
                    product.Variants = await GetProductVariantsAsync(id);

                    return product;
                }

                _logger.LogWarning($"Product with ID {id} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving product with ID {id}");
                throw;
            }
        }

        public async Task<Product> CreateProductAsync(CreateProductDto productDto)
        {
            // Check if product name already exists
            if (await ProductNameExistsAsync(productDto.Name))
            {
                throw new InvalidOperationException($"Product '{productDto.Name}' already exists");
            }

            // Verify category exists
            var category = await GetCategoryByIdAsync(productDto.CategoryId);
            if (category == null)
            {
                throw new InvalidOperationException($"Category ID {productDto.CategoryId} not found");
            }

            using var connection = _db.GetConnection();
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Insert product
                string productQuery = @"
                    INSERT INTO products (name, description, category_id, supplier_id, notes, is_active)
                    VALUES (@name, @description, @categoryId, @supplierId, @notes, 1);
                    SELECT LAST_INSERT_ID();";

                int productId;
                using (var command = new MySqlCommand(productQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@name", productDto.Name);
                    command.Parameters.AddWithValue("@description", productDto.Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@categoryId", productDto.CategoryId);
                    command.Parameters.AddWithValue("@supplierId", productDto.SupplierId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@notes", productDto.Notes ?? (object)DBNull.Value);

                    productId = Convert.ToInt32(await command.ExecuteScalarAsync());
                }

                // Insert variants
                foreach (var variantDto in productDto.Variants)
                {
                    string variantQuery = @"
                        INSERT INTO product_variants 
                            (product_id, size, color, class_type, unit_of_measure, 
                             quantity_in_stock, price_per_unit, price_per_length, 
                             reorder_level, location, notes, is_active)
                        VALUES 
                            (@productId, @size, @color, @classType, @unitOfMeasure,
                             @quantityInStock, @pricePerUnit, @pricePerLength,
                             @reorderLevel, @location, @notes, 1)";

                    using (var command = new MySqlCommand(variantQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@productId", productId);
                        command.Parameters.AddWithValue("@size", variantDto.Size ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@color", variantDto.Color ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@classType", variantDto.ClassType ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@unitOfMeasure", variantDto.UnitOfMeasure);
                        command.Parameters.AddWithValue("@quantityInStock", variantDto.QuantityInStock);
                        command.Parameters.AddWithValue("@pricePerUnit", variantDto.PricePerUnit);
                        command.Parameters.AddWithValue("@pricePerLength", variantDto.PricePerLength ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@reorderLevel", variantDto.ReorderLevel);
                        command.Parameters.AddWithValue("@location", variantDto.Location ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@notes", variantDto.Notes ?? (object)DBNull.Value);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                await transaction.CommitAsync();
                _logger.LogInformation($"Product created with ID {productId}");

                return (await GetProductByIdAsync(productId))!;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating product");
                throw;
            }
        }

        public async Task<bool> UpdateProductAsync(int id, UpdateProductDto productDto)
        {
            // Check if product name already exists for another product
            if (await ProductNameExistsAsync(productDto.Name, id))
            {
                throw new InvalidOperationException($"Product '{productDto.Name}' already exists");
            }

            // Verify category exists
            var category = await GetCategoryByIdAsync(productDto.CategoryId);
            if (category == null)
            {
                throw new InvalidOperationException($"Category ID {productDto.CategoryId} not found");
            }

            string query = @"
                UPDATE products 
                SET name = @name, 
                    description = @description, 
                    category_id = @categoryId,
                    supplier_id = @supplierId,
                    notes = @notes,
                    updated_at = CURRENT_TIMESTAMP
                WHERE product_id = @id AND is_active = 1";

            try
            {
                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@name", productDto.Name),
                    new MySqlParameter("@description", productDto.Description ?? (object)DBNull.Value),
                    new MySqlParameter("@categoryId", productDto.CategoryId),
                    new MySqlParameter("@supplierId", productDto.SupplierId ?? (object)DBNull.Value),
                    new MySqlParameter("@notes", productDto.Notes ?? (object)DBNull.Value)
                };

                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Product {id} updated successfully");
                    return true;
                }

                _logger.LogWarning($"Product {id} not found or inactive");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating product {id}");
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            // Soft delete product (variants will be handled by trigger or cascading)
            string query = "UPDATE products SET is_active = 0, updated_at = CURRENT_TIMESTAMP WHERE product_id = @id";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Product {id} deleted (soft delete)");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting product {id}");
                throw;
            }
        }

        public async Task<bool> RestoreProductAsync(int id)
        {
            string query = "UPDATE products SET is_active = 1, updated_at = CURRENT_TIMESTAMP WHERE product_id = @id";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Product {id} restored");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring product {id}");
                throw;
            }
        }

        #endregion

        #region Product Variants

        public async Task<List<ProductVariant>> GetProductVariantsAsync(int productId)
        {
            var variants = new List<ProductVariant>();
            string query = @"
                SELECT * FROM product_variants 
                WHERE product_id = @productId AND is_active = 1
                ORDER BY 
                    CASE 
                        WHEN size IS NOT NULL THEN size 
                        WHEN color IS NOT NULL THEN color 
                        ELSE class_type 
                    END";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@productId", productId);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    variants.Add(MapToVariant(reader));
                }

                return variants;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving variants for product {productId}");
                throw;
            }
        }

        public async Task<ProductVariant?> GetVariantByIdAsync(int variantId)
        {
            string query = "SELECT * FROM product_variants WHERE variant_id = @variantId";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@variantId", variantId);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return MapToVariant(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving variant with ID {variantId}");
                throw;
            }
        }

        public async Task<ProductVariant> AddVariantAsync(int productId, CreateProductVariantDto variantDto)
        {
            // Check if product exists
            var product = await GetProductByIdAsync(productId);
            if (product == null)
            {
                throw new InvalidOperationException($"Product with ID {productId} not found");
            }

            // Check for duplicate variant combination
            if (await VariantCombinationExistsAsync(productId, variantDto.Size, variantDto.Color, variantDto.ClassType))
            {
                throw new InvalidOperationException("Variant with same size/color/class already exists");
            }

            string query = @"
                INSERT INTO product_variants 
                    (product_id, size, color, class_type, unit_of_measure, 
                     quantity_in_stock, price_per_unit, price_per_length, 
                     reorder_level, location, notes, is_active)
                VALUES 
                    (@productId, @size, @color, @classType, @unitOfMeasure,
                     @quantityInStock, @pricePerUnit, @pricePerLength,
                     @reorderLevel, @location, @notes, 1);
                SELECT LAST_INSERT_ID();";

            try
            {
                var parameters = new[]
                {
                    new MySqlParameter("@productId", productId),
                    new MySqlParameter("@size", variantDto.Size ?? (object)DBNull.Value),
                    new MySqlParameter("@color", variantDto.Color ?? (object)DBNull.Value),
                    new MySqlParameter("@classType", variantDto.ClassType ?? (object)DBNull.Value),
                    new MySqlParameter("@unitOfMeasure", variantDto.UnitOfMeasure),
                    new MySqlParameter("@quantityInStock", variantDto.QuantityInStock),
                    new MySqlParameter("@pricePerUnit", variantDto.PricePerUnit),
                    new MySqlParameter("@pricePerLength", variantDto.PricePerLength ?? (object)DBNull.Value),
                    new MySqlParameter("@reorderLevel", variantDto.ReorderLevel),
                    new MySqlParameter("@location", variantDto.Location ?? (object)DBNull.Value),
                    new MySqlParameter("@notes", variantDto.Notes ?? (object)DBNull.Value)
                };

                var variantId = Convert.ToInt32(await _db.ExecuteScalarAsync(query, parameters));
                _logger.LogInformation($"Variant added with ID {variantId} for product {productId}");

                return (await GetVariantByIdAsync(variantId))!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding variant for product {productId}");
                throw;
            }
        }

        public async Task<bool> UpdateVariantAsync(int variantId, UpdateProductVariantDto variantDto)
        {
            // Check if variant exists
            var existingVariant = await GetVariantByIdAsync(variantId);
            if (existingVariant == null)
            {
                return false;
            }

            // Check for duplicate variant combination (excluding this variant)
            if (await VariantCombinationExistsAsync(existingVariant.ProductId,
                variantDto.Size, variantDto.Color, variantDto.ClassType, variantId))
            {
                throw new InvalidOperationException("Variant with same size/color/class already exists");
            }

            string query = @"
                UPDATE product_variants 
                SET size = @size,
                    color = @color,
                    class_type = @classType,
                    unit_of_measure = @unitOfMeasure,
                    quantity_in_stock = @quantityInStock,
                    price_per_unit = @pricePerUnit,
                    price_per_length = @pricePerLength,
                    reorder_level = @reorderLevel,
                    location = @location,
                    is_active = @isActive,
                    notes = @notes,
                    updated_at = CURRENT_TIMESTAMP
                WHERE variant_id = @variantId";

            try
            {
                var parameters = new[]
                {
                    new MySqlParameter("@variantId", variantId),
                    new MySqlParameter("@size", variantDto.Size ?? (object)DBNull.Value),
                    new MySqlParameter("@color", variantDto.Color ?? (object)DBNull.Value),
                    new MySqlParameter("@classType", variantDto.ClassType ?? (object)DBNull.Value),
                    new MySqlParameter("@unitOfMeasure", variantDto.UnitOfMeasure),
                    new MySqlParameter("@quantityInStock", variantDto.QuantityInStock),
                    new MySqlParameter("@pricePerUnit", variantDto.PricePerUnit),
                    new MySqlParameter("@pricePerLength", variantDto.PricePerLength ?? (object)DBNull.Value),
                    new MySqlParameter("@reorderLevel", variantDto.ReorderLevel),
                    new MySqlParameter("@location", variantDto.Location ?? (object)DBNull.Value),
                    new MySqlParameter("@isActive", variantDto.IsActive),
                    new MySqlParameter("@notes", variantDto.Notes ?? (object)DBNull.Value)
                };

                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Variant {variantId} updated successfully");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating variant {variantId}");
                throw;
            }
        }

        public async Task<bool> DeleteVariantAsync(int variantId)
        {
            string query = "UPDATE product_variants SET is_active = 0, updated_at = CURRENT_TIMESTAMP WHERE variant_id = @variantId";

            try
            {
                var parameters = new[] { new MySqlParameter("@variantId", variantId) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Variant {variantId} deleted");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting variant {variantId}");
                throw;
            }
        }

        public async Task<bool> UpdateVariantStockAsync(int variantId, decimal quantityChange, string reason)
        {
            try
            {
                // Update stock
                string updateQuery = @"
            UPDATE product_variants 
            SET quantity_in_stock = quantity_in_stock + @change,
                updated_at = CURRENT_TIMESTAMP
            WHERE variant_id = @variantId";

                var parameters = new[]
                {
            new MySqlParameter("@variantId", variantId),
            new MySqlParameter("@change", quantityChange)
        };

                var rowsAffected = await _db.ExecuteNonQueryAsync(updateQuery, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Stock updated for variant {variantId}: {quantityChange}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating stock for variant {variantId}");
                throw;
            }
        }
        #endregion

        #region Search & Filters

        public async Task<PaginatedResponse<Product>> SearchProductsAsync(ProductSearchDto searchDto)
        {
            var response = new PaginatedResponse<Product>
            {
                PageNumber = searchDto.PageNumber,
                PageSize = searchDto.PageSize
            };

            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            // Base query
            string baseQuery = @"
                FROM products p
                LEFT JOIN lookup l ON p.category_id = l.lookup_id AND l.type = 'category'
                LEFT JOIN supplier s ON p.supplier_id = s.supplier_id
                WHERE 1=1";

            // Apply filters
            if (!searchDto.IncludeInactive)
            {
                conditions.Add("p.is_active = 1");
            }

            if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
            {
                conditions.Add("(p.name LIKE @searchTerm OR p.description LIKE @searchTerm)");
                parameters.Add(new MySqlParameter("@searchTerm", $"%{searchDto.SearchTerm}%"));
            }

            if (searchDto.CategoryId.HasValue)
            {
                conditions.Add("p.category_id = @categoryId");
                parameters.Add(new MySqlParameter("@categoryId", searchDto.CategoryId.Value));
            }

            if (searchDto.SupplierId.HasValue)
            {
                conditions.Add("p.supplier_id = @supplierId");
                parameters.Add(new MySqlParameter("@supplierId", searchDto.SupplierId.Value));
            }

            if (searchDto.InStock.HasValue && searchDto.InStock.Value)
            {
                conditions.Add(@"EXISTS (
                    SELECT 1 FROM product_variants pv 
                    WHERE pv.product_id = p.product_id AND pv.quantity_in_stock > 0
                )");
            }

            if (searchDto.LowStock.HasValue && searchDto.LowStock.Value)
            {
                conditions.Add(@"EXISTS (
                    SELECT 1 FROM product_variants pv 
                    WHERE pv.product_id = p.product_id 
                    AND pv.quantity_in_stock <= pv.reorder_level
                    AND pv.quantity_in_stock > 0
                )");
            }

            if (searchDto.MinPrice.HasValue)
            {
                conditions.Add(@"EXISTS (
                    SELECT 1 FROM product_variants pv 
                    WHERE pv.product_id = p.product_id AND pv.price_per_unit >= @minPrice
                )");
                parameters.Add(new MySqlParameter("@minPrice", searchDto.MinPrice.Value));
            }

            if (searchDto.MaxPrice.HasValue)
            {
                conditions.Add(@"EXISTS (
                    SELECT 1 FROM product_variants pv 
                    WHERE pv.product_id = p.product_id AND pv.price_per_unit <= @maxPrice
                )");
                parameters.Add(new MySqlParameter("@maxPrice", searchDto.MaxPrice.Value));
            }

            string whereClause = conditions.Count > 0 ? " AND " + string.Join(" AND ", conditions) : "";

            try
            {
                // Get total count
                string countQuery = $"SELECT COUNT(DISTINCT p.product_id) {baseQuery} {whereClause}";
                response.TotalRecords = Convert.ToInt32(await _db.ExecuteScalarAsync(countQuery, parameters.ToArray()));

                // Get paginated data
                int offset = (searchDto.PageNumber - 1) * searchDto.PageSize;
                string dataQuery = $@"
                    SELECT p.*, l.value as category_name, s.name as supplier_name
                    {baseQuery} {whereClause}
                    ORDER BY p.name 
                    LIMIT @pageSize OFFSET @offset";

                var allParameters = parameters.ToList();
                allParameters.Add(new MySqlParameter("@pageSize", searchDto.PageSize));
                allParameters.Add(new MySqlParameter("@offset", offset));

                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(dataQuery, connection);
                command.Parameters.AddRange(allParameters.ToArray());

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    response.Data.Add(MapToProduct(reader));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                throw;
            }
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId, bool includeInactive = false)
        {
            var products = new List<Product>();
            string query = @"
                SELECT p.*, l.value as category_name, s.name as supplier_name
                FROM products p
                LEFT JOIN lookup l ON p.category_id = l.lookup_id AND l.type = 'category'
                LEFT JOIN supplier s ON p.supplier_id = s.supplier_id
                WHERE p.category_id = @categoryId
                " + (includeInactive ? "" : "AND p.is_active = 1") + @"
                ORDER BY p.name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@categoryId", categoryId);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    products.Add(MapToProduct(reader));
                }

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving products for category {categoryId}");
                throw;
            }
        }

        public async Task<List<Product>> GetProductsBySupplierAsync(int supplierId, bool includeInactive = false)
        {
            var products = new List<Product>();
            string query = @"
                SELECT p.*, l.value as category_name, s.name as supplier_name
                FROM products p
                LEFT JOIN lookup l ON p.category_id = l.lookup_id AND l.type = 'category'
                LEFT JOIN supplier s ON p.supplier_id = s.supplier_id
                WHERE p.supplier_id = @supplierId
                " + (includeInactive ? "" : "AND p.is_active = 1") + @"
                ORDER BY p.name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@supplierId", supplierId);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    products.Add(MapToProduct(reader));
                }

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving products for supplier {supplierId}");
                throw;
            }
        }

        public async Task<List<ProductWithStock>> GetProductsWithStockSummaryAsync()
        {
            var products = new List<ProductWithStock>();
            string query = @"
                SELECT 
                    p.product_id,
                    p.name,
                    p.description,
                    l.value as category_name,
                    s.name as supplier_name,
                    COUNT(pv.variant_id) as variant_count,
                    COALESCE(SUM(pv.quantity_in_stock), 0) as total_stock,
                    COALESCE(SUM(pv.quantity_in_stock * pv.price_per_unit), 0) as total_value,
                    p.is_active
                FROM products p
                LEFT JOIN lookup l ON p.category_id = l.lookup_id AND l.type = 'category'
                LEFT JOIN supplier s ON p.supplier_id = s.supplier_id
                LEFT JOIN product_variants pv ON p.product_id = pv.product_id AND pv.is_active = 1
                GROUP BY p.product_id
                ORDER BY total_value DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    products.Add(new ProductWithStock
                    {
                        ProductId = reader.GetInt32("product_id"),
                        Name = reader.GetString("name"),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                        CategoryName = reader.IsDBNull(reader.GetOrdinal("category_name")) ? "Uncategorized" : reader.GetString("category_name"),
                        SupplierName = reader.IsDBNull(reader.GetOrdinal("supplier_name")) ? "No Supplier" : reader.GetString("supplier_name"),
                        VariantCount = reader.GetInt32("variant_count"),
                        TotalStock = reader.GetDecimal("total_stock"),
                        TotalValue = reader.GetDecimal("total_value"),
                        IsActive = reader.GetBoolean("is_active")
                    });
                }

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products with stock summary");
                throw;
            }
        }

        #endregion

        #region Stock Management

        public async Task<List<ProductVariant>> GetLowStockItemsAsync(decimal threshold = 10)
        {
            var variants = new List<ProductVariant>();
            string query = @"
                SELECT pv.*, p.name as product_name
                FROM product_variants pv
                JOIN products p ON pv.product_id = p.product_id
                WHERE pv.quantity_in_stock <= pv.reorder_level
                AND pv.quantity_in_stock > 0
                AND pv.is_active = 1
                AND p.is_active = 1
                ORDER BY (pv.reorder_level - pv.quantity_in_stock) DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    variants.Add(MapToVariant(reader));
                }

                return variants;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving low stock items");
                throw;
            }
        }

        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            string query = @"
                SELECT COALESCE(SUM(quantity_in_stock * price_per_unit), 0)
                FROM product_variants
                WHERE is_active = 1";

            try
            {
                var result = await _db.ExecuteScalarAsync(query);
                return result != null ? Convert.ToDecimal(result) : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total inventory value");
                throw;
            }
        }

        public async Task<List<ProductVariant>> GetOutOfStockItemsAsync()
        {
            var variants = new List<ProductVariant>();
            string query = @"
                SELECT pv.*, p.name as product_name
                FROM product_variants pv
                JOIN products p ON pv.product_id = p.product_id
                WHERE pv.quantity_in_stock = 0
                AND pv.is_active = 1
                AND p.is_active = 1
                ORDER BY p.name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    variants.Add(MapToVariant(reader));
                }

                return variants;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving out of stock items");
                throw;
            }
        }

        #endregion

        #region Categories (from lookup)

        public async Task<List<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = new List<CategoryDto>();
            string query = @"
                SELECT lookup_id, value 
                FROM lookup 
                WHERE type = 'category' AND is_active = 1
                ORDER BY value";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    categories.Add(new CategoryDto
                    {
                        LookupId = reader.GetInt32("lookup_id"),
                        Value = reader.GetString("value")
                    });
                }

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories from lookup");
                throw;
            }
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int categoryId)
        {
            string query = @"
                SELECT lookup_id, value 
                FROM lookup 
                WHERE lookup_id = @categoryId AND type = 'category' AND is_active = 1";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@categoryId", categoryId);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new CategoryDto
                    {
                        LookupId = reader.GetInt32("lookup_id"),
                        Value = reader.GetString("value")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving category with ID {categoryId}");
                throw;
            }
        }

        #endregion

        #region Validation

        public async Task<bool> ProductNameExistsAsync(string name, int? excludeProductId = null)
        {
            string query = excludeProductId.HasValue
                ? "SELECT COUNT(*) FROM products WHERE name = @name AND product_id != @excludeId"
                : "SELECT COUNT(*) FROM products WHERE name = @name";

            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter("@name", name)
            };

            if (excludeProductId.HasValue)
            {
                parameters.Add(new MySqlParameter("@excludeId", excludeProductId.Value));
            }

            var count = Convert.ToInt32(await _db.ExecuteScalarAsync(query, parameters.ToArray()));
            return count > 0;
        }

        public async Task<bool> VariantCombinationExistsAsync(int productId, string? size, string? color, string? classType, int? excludeVariantId = null)
        {
            string query = @"
                SELECT COUNT(*) FROM product_variants 
                WHERE product_id = @productId 
                AND ((size IS NULL AND @size IS NULL) OR size = @size)
                AND ((color IS NULL AND @color IS NULL) OR color = @color)
                AND ((class_type IS NULL AND @classType IS NULL) OR class_type = @classType)";

            if (excludeVariantId.HasValue)
            {
                query += " AND variant_id != @excludeId";
            }

            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter("@productId", productId),
                new MySqlParameter("@size", size ?? (object)DBNull.Value),
                new MySqlParameter("@color", color ?? (object)DBNull.Value),
                new MySqlParameter("@classType", classType ?? (object)DBNull.Value)
            };

            if (excludeVariantId.HasValue)
            {
                parameters.Add(new MySqlParameter("@excludeId", excludeVariantId.Value));
            }

            var count = Convert.ToInt32(await _db.ExecuteScalarAsync(query, parameters.ToArray()));
            return count > 0;
        }

        #endregion

        #region Helper Methods

        private Product MapToProduct(DbDataReader reader)
        {
            return new Product
            {
                ProductId = reader.GetInt32(reader.GetOrdinal("product_id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(reader.GetOrdinal("description"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("description")),
                CategoryId = reader.IsDBNull(reader.GetOrdinal("category_id"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("category_id")),
                CategoryName = reader.IsDBNull(reader.GetOrdinal("category_name"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("category_name")),
                SupplierId = reader.IsDBNull(reader.GetOrdinal("supplier_id"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("supplier_id")),
                SupplierName = reader.IsDBNull(reader.GetOrdinal("supplier_name"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("supplier_name")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                // ✅ FIX: Check if notes column exists before reading
                Notes = HasColumn(reader, "notes") && !reader.IsDBNull(reader.GetOrdinal("notes"))
                    ? reader.GetString(reader.GetOrdinal("notes"))
                    : null,
                Variants = new List<ProductVariant>()
            };
        }
        private bool HasColumn(DbDataReader reader, string columnName)
        {
            try
            {
                return reader.GetOrdinal(columnName) >= 0;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }
        private ProductVariant MapToVariant(DbDataReader reader)
        {
            return new ProductVariant
            {
                VariantId = reader.GetInt32(reader.GetOrdinal("variant_id")),
                ProductId = reader.GetInt32(reader.GetOrdinal("product_id")),
                Size = reader.IsDBNull(reader.GetOrdinal("size")) ? null : reader.GetString(reader.GetOrdinal("size")),
                Color = reader.IsDBNull(reader.GetOrdinal("color")) ? null : reader.GetString(reader.GetOrdinal("color")),
                ClassType = reader.IsDBNull(reader.GetOrdinal("class_type")) ? null : reader.GetString(reader.GetOrdinal("class_type")),
                UnitOfMeasure = reader.GetString(reader.GetOrdinal("unit_of_measure")),
                QuantityInStock = reader.GetDecimal(reader.GetOrdinal("quantity_in_stock")),
                PricePerUnit = reader.GetDecimal(reader.GetOrdinal("price_per_unit")),
                PricePerLength = reader.IsDBNull(reader.GetOrdinal("price_per_length")) ? null : reader.GetDecimal(reader.GetOrdinal("price_per_length")),
                ReorderLevel = reader.GetDecimal(reader.GetOrdinal("reorder_level")),
                Location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString(reader.GetOrdinal("location")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes"))
            };
        }

        #endregion
    }
}