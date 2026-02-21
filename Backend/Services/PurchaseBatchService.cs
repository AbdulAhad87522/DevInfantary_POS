using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;
using System.Text;

namespace HardwareStoreAPI.Services
{
    public class PurchaseBatchService : IPurchaseBatchService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<PurchaseBatchService> _logger;

        // ✅ This constructor must match exactly
        public PurchaseBatchService(ILogger<PurchaseBatchService> logger)
        {
            _db = DatabaseHelper.Instance;  // Static access - not injected
            _logger = logger;
        }

        #region Batch CRUD

        public async Task<int> GetNextBatchIdAsync()
        {
            string query = "SELECT COALESCE(MAX(batch_id), 0) + 1 FROM purchase_batches;";
            var result = await _db.ExecuteScalarAsync(query);
            return result != null ? Convert.ToInt32(result) : 1;
        }

        public async Task<int> GetNextItemIdAsync()
        {
            string query = "SELECT COALESCE(MAX(purchase_batch_item_id), 0) + 1 FROM purchase_batch_items;";
            var result = await _db.ExecuteScalarAsync(query);
            return result != null ? Convert.ToInt32(result) : 1;
        }

        public async Task<List<PurchaseBatch>> GetAllBatchesAsync()
        {
            var batches = new List<PurchaseBatch>();

            // ✅ Use COALESCE to handle NULL values
            string query = @"
        SELECT 
            pb.batch_id,
            pb.supplier_id,
            pb.BatchName,
            COALESCE(pb.total_price, 0) as total_price,
            COALESCE(pb.paid, 0) as paid,
            COALESCE(pb.status, 'Pending') as status,
            pb.CreatedAt,
            pb.UpdatedAt,
            COALESCE(s.name, 'Unknown') as supplier_name,
            COALESCE(pb.total_price, 0) - COALESCE(pb.paid, 0) as remaining
        FROM purchase_batches pb
        LEFT JOIN supplier s ON pb.supplier_id = s.supplier_id
        ORDER BY pb.batch_id DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    batches.Add(new PurchaseBatch
                    {
                        BatchId = reader.GetInt32("batch_id"),
                        SupplierId = reader.GetInt32("supplier_id"),
                        BatchName = reader.GetString("BatchName"),
                        TotalPrice = reader.GetDecimal("total_price"),
                        Paid = reader.GetDecimal("paid"),
                        Status = reader.GetString("status"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ?
                            reader.GetDateTime("CreatedAt") :
                            reader.GetDateTime("UpdatedAt"),
                        SupplierName = reader.GetString("supplier_name"),
                        Items = new List<PurchaseBatchItem>()
                    });
                }

                _logger?.LogInformation($"Retrieved {batches.Count} purchase batches");
                return batches;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving all batches");
                throw; // This will be caught by your global handler
            }
        }
        public async Task<PaginatedResponse<PurchaseBatch>> GetBatchesPaginatedAsync(int pageNumber, int pageSize, PurchaseBatchSearchDto? filters = null)
        {
            var response = new PaginatedResponse<PurchaseBatch>
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            string baseQuery = @"
                FROM purchase_batches pb
                LEFT JOIN supplier s ON pb.supplier_id = s.supplier_id
                WHERE 1=1";

            if (filters != null)
            {
                if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                {
                    conditions.Add("(pb.BatchName LIKE @searchTerm OR s.name LIKE @searchTerm)");
                    parameters.Add(new MySqlParameter("@searchTerm", $"%{filters.SearchTerm}%"));
                }

                if (filters.SupplierId.HasValue)
                {
                    conditions.Add("pb.supplier_id = @supplierId");
                    parameters.Add(new MySqlParameter("@supplierId", filters.SupplierId.Value));
                }

                if (!string.IsNullOrWhiteSpace(filters.Status))
                {
                    conditions.Add("pb.status = @status");
                    parameters.Add(new MySqlParameter("@status", filters.Status));
                }

                if (filters.StartDate.HasValue)
                {
                    conditions.Add("pb.CreatedAt >= @startDate");
                    parameters.Add(new MySqlParameter("@startDate", filters.StartDate.Value));
                }

                if (filters.EndDate.HasValue)
                {
                    conditions.Add("pb.CreatedAt <= @endDate");
                    parameters.Add(new MySqlParameter("@endDate", filters.EndDate.Value));
                }

                if (filters.MinTotal.HasValue)
                {
                    conditions.Add("pb.total_price >= @minTotal");
                    parameters.Add(new MySqlParameter("@minTotal", filters.MinTotal.Value));
                }

                if (filters.MaxTotal.HasValue)
                {
                    conditions.Add("pb.total_price <= @maxTotal");
                    parameters.Add(new MySqlParameter("@maxTotal", filters.MaxTotal.Value));
                }

                if (filters.HasOutstanding.HasValue && filters.HasOutstanding.Value)
                {
                    conditions.Add("pb.total_price > pb.paid");
                }
            }

            string whereClause = conditions.Count > 0 ? " AND " + string.Join(" AND ", conditions) : "";

            try
            {
                // Get total count
                string countQuery = $"SELECT COUNT(*) {baseQuery} {whereClause}";
                response.TotalRecords = Convert.ToInt32(await _db.ExecuteScalarAsync(countQuery, parameters.ToArray()));

                // Get paginated data
                int offset = (pageNumber - 1) * pageSize;
                string dataQuery = $@"
                    SELECT pb.*, s.name as supplier_name,
                           (pb.total_price - pb.paid) as remaining
                    {baseQuery} {whereClause}
                    ORDER BY pb.batch_id DESC
                    LIMIT @pageSize OFFSET @offset";

                var allParameters = parameters.ToList();
                allParameters.Add(new MySqlParameter("@pageSize", pageSize));
                allParameters.Add(new MySqlParameter("@offset", offset));

                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(dataQuery, connection);
                command.Parameters.AddRange(allParameters.ToArray());

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    response.Data.Add(MapBatchFromReader(reader));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated batches");
                throw;
            }
        }

        public async Task<PurchaseBatch?> GetBatchByIdAsync(int id)
        {
            string query = @"
                SELECT pb.*, s.name as supplier_name,
                       (pb.total_price - pb.paid) as remaining
                FROM purchase_batches pb
                LEFT JOIN supplier s ON pb.supplier_id = s.supplier_id
                WHERE pb.batch_id = @id";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var batch = MapBatchFromReader(reader);
                    reader.Close();

                    // Get items for this batch
                    batch.Items = await GetBatchItemsAsync(id);

                    return batch;
                }

                _logger.LogWarning($"Purchase batch with ID {id} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving batch with ID {id}");
                throw;
            }
        }

        public async Task<PurchaseBatch> CreateBatchAsync(CreatePurchaseBatchDto batchDto)
        {
            // Check if batch name already exists
            if (await BatchNameExistsAsync(batchDto.BatchName))
            {
                throw new InvalidOperationException($"Batch name '{batchDto.BatchName}' already exists");
            }

            // Verify supplier exists
            string checkSupplier = "SELECT COUNT(*) FROM supplier WHERE supplier_id = @supplierId";
            var supplierCount = Convert.ToInt32(await _db.ExecuteScalarAsync(checkSupplier,
                new[] { new MySqlParameter("@supplierId", batchDto.SupplierId) }));

            if (supplierCount == 0)
            {
                throw new InvalidOperationException($"Supplier with ID {batchDto.SupplierId} not found");
            }

            using var connection = _db.GetConnection();
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Get next batch ID
                int batchId = await GetNextBatchIdAsync();

                // Insert batch
                string batchQuery = @"
                    INSERT INTO purchase_batches 
                    (batch_id, supplier_id, BatchName, total_price, paid, status, CreatedAt)
                    VALUES 
                    (@batchId, @supplierId, @batchName, @totalPrice, @paid, @status, NOW())";

                using (var batchCmd = new MySqlCommand(batchQuery, connection, transaction))
                {
                    batchCmd.Parameters.AddWithValue("@batchId", batchId);
                    batchCmd.Parameters.AddWithValue("@supplierId", batchDto.SupplierId);
                    batchCmd.Parameters.AddWithValue("@batchName", batchDto.BatchName);
                    batchCmd.Parameters.AddWithValue("@totalPrice", batchDto.TotalPrice);
                    batchCmd.Parameters.AddWithValue("@paid", batchDto.Paid);
                    batchCmd.Parameters.AddWithValue("@status", batchDto.Status);

                    await batchCmd.ExecuteNonQueryAsync();
                }

                // Insert items and update stock
                foreach (var itemDto in batchDto.Items)
                {
                    int itemId = await GetNextItemIdAsync();
                    decimal lineTotal = itemDto.LineTotal ?? (itemDto.QuantityReceived * itemDto.CostPrice);

                    string itemQuery = @"
                        INSERT INTO purchase_batch_items 
                        (purchase_batch_item_id, purchase_batch_id, variant_id, 
                         quantity_recieved, cost_price, CreatedAt)
                        VALUES 
                        (@itemId, @batchId, @variantId, @quantity, @costPrice, NOW());
                        
                        UPDATE product_variants 
                        SET quantity_in_stock = quantity_in_stock + @quantity,
                            updated_at = CURRENT_TIMESTAMP
                        WHERE variant_id = @variantId;";

                    using (var itemCmd = new MySqlCommand(itemQuery, connection, transaction))
                    {
                        itemCmd.Parameters.AddWithValue("@itemId", itemId);
                        itemCmd.Parameters.AddWithValue("@batchId", batchId);
                        itemCmd.Parameters.AddWithValue("@variantId", itemDto.VariantId);
                        itemCmd.Parameters.AddWithValue("@quantity", itemDto.QuantityReceived);
                        itemCmd.Parameters.AddWithValue("@costPrice", itemDto.CostPrice);

                        await itemCmd.ExecuteNonQueryAsync();
                    }
                }

                await transaction.CommitAsync();
                _logger.LogInformation($"Purchase batch created with ID {batchId}");

                return (await GetBatchByIdAsync(batchId))!;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating purchase batch");
                throw;
            }
        }

        public async Task<bool> UpdateBatchAsync(int id, UpdatePurchaseBatchDto batchDto)
        {
            // Check if batch name already exists for another batch
            if (await BatchNameExistsAsync(batchDto.BatchName, id))
            {
                throw new InvalidOperationException($"Batch name '{batchDto.BatchName}' already exists");
            }

            string query = @"
                UPDATE purchase_batches 
                SET supplier_id = @supplierId,
                    BatchName = @batchName,
                    total_price = @totalPrice,
                    paid = @paid,
                    status = @status,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE batch_id = @id";

            try
            {
                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@supplierId", batchDto.SupplierId),
                    new MySqlParameter("@batchName", batchDto.BatchName),
                    new MySqlParameter("@totalPrice", batchDto.TotalPrice),
                    new MySqlParameter("@paid", batchDto.Paid),
                    new MySqlParameter("@status", batchDto.Status)
                };

                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Purchase batch {id} updated successfully");
                    return true;
                }

                _logger.LogWarning($"Purchase batch {id} not found");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating purchase batch {id}");
                throw;
            }
        }

        public async Task<bool> DeleteBatchAsync(int id)
        {
            using var connection = _db.GetConnection();
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // First reverse stock from items
                string reverseStockQuery = @"
                    UPDATE product_variants pv
                    INNER JOIN purchase_batch_items pbi ON pv.variant_id = pbi.variant_id
                    SET pv.quantity_in_stock = pv.quantity_in_stock - pbi.quantity_recieved,
                        pv.updated_at = CURRENT_TIMESTAMP
                    WHERE pbi.purchase_batch_id = @batchId";

                using (var reverseCmd = new MySqlCommand(reverseStockQuery, connection, transaction))
                {
                    reverseCmd.Parameters.AddWithValue("@batchId", id);
                    await reverseCmd.ExecuteNonQueryAsync();
                }

                // Delete items
                string deleteItems = "DELETE FROM purchase_batch_items WHERE purchase_batch_id = @batchId";
                using (var deleteItemsCmd = new MySqlCommand(deleteItems, connection, transaction))
                {
                    deleteItemsCmd.Parameters.AddWithValue("@batchId", id);
                    await deleteItemsCmd.ExecuteNonQueryAsync();
                }

                // Delete batch
                string deleteBatch = "DELETE FROM purchase_batches WHERE batch_id = @batchId";
                using (var deleteBatchCmd = new MySqlCommand(deleteBatch, connection, transaction))
                {
                    deleteBatchCmd.Parameters.AddWithValue("@batchId", id);
                    await deleteBatchCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                _logger.LogInformation($"Purchase batch {id} deleted successfully");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error deleting purchase batch {id}");
                throw;
            }
        }

        #endregion

        #region Batch Items

        public async Task<List<PurchaseBatchItem>> GetBatchItemsAsync(int batchId)
        {
            var items = new List<PurchaseBatchItem>();
            string query = @"
                SELECT pbi.*, 
                       p.name as product_name,
                       pv.size,
                       pv.class_type,
                       pv.price_per_unit as sale_price,
                       (pbi.quantity_recieved * pbi.cost_price) as line_total
                FROM purchase_batch_items pbi
                INNER JOIN product_variants pv ON pbi.variant_id = pv.variant_id
                INNER JOIN products p ON pv.product_id = p.product_id
                WHERE pbi.purchase_batch_id = @batchId
                ORDER BY pbi.purchase_batch_item_id";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@batchId", batchId);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    items.Add(MapItemFromReader(reader));
                }

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving items for batch {batchId}");
                throw;
            }
        }

        public async Task<PurchaseBatchItem?> GetBatchItemByIdAsync(int itemId)
        {
            string query = @"
                SELECT pbi.*, 
                       p.name as product_name,
                       pv.size,
                       pv.class_type,
                       pv.price_per_unit as sale_price,
                       (pbi.quantity_recieved * pbi.cost_price) as line_total
                FROM purchase_batch_items pbi
                INNER JOIN product_variants pv ON pbi.variant_id = pv.variant_id
                INNER JOIN products p ON pv.product_id = p.product_id
                WHERE pbi.purchase_batch_item_id = @itemId";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@itemId", itemId);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return MapItemFromReader(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving batch item {itemId}");
                throw;
            }
        }

        public async Task<PurchaseBatchItem> AddBatchItemAsync(int batchId, CreatePurchaseBatchItemDto itemDto)
        {
            // Verify batch exists
            var batch = await GetBatchByIdAsync(batchId);
            if (batch == null)
            {
                throw new InvalidOperationException($"Batch with ID {batchId} not found");
            }

            // Verify variant exists
            string checkVariant = "SELECT COUNT(*) FROM product_variants WHERE variant_id = @variantId";
            var variantCount = Convert.ToInt32(await _db.ExecuteScalarAsync(checkVariant,
                new[] { new MySqlParameter("@variantId", itemDto.VariantId) }));

            if (variantCount == 0)
            {
                throw new InvalidOperationException($"Variant with ID {itemDto.VariantId} not found");
            }

            using var connection = _db.GetConnection();
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                int itemId = await GetNextItemIdAsync();
                decimal lineTotal = itemDto.LineTotal ?? (itemDto.QuantityReceived * itemDto.CostPrice);

                string query = @"
                    INSERT INTO purchase_batch_items 
                    (purchase_batch_item_id, purchase_batch_id, variant_id, 
                     quantity_recieved, cost_price, CreatedAt)
                    VALUES 
                    (@itemId, @batchId, @variantId, @quantity, @costPrice, NOW());
                    
                    UPDATE product_variants 
                    SET quantity_in_stock = quantity_in_stock + @quantity,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE variant_id = @variantId;
                    
                    UPDATE purchase_batches
                    SET total_price = total_price + @lineTotal,
                        UpdatedAt = CURRENT_TIMESTAMP
                    WHERE batch_id = @batchId;";

                using (var cmd = new MySqlCommand(query, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@itemId", itemId);
                    cmd.Parameters.AddWithValue("@batchId", batchId);
                    cmd.Parameters.AddWithValue("@variantId", itemDto.VariantId);
                    cmd.Parameters.AddWithValue("@quantity", itemDto.QuantityReceived);
                    cmd.Parameters.AddWithValue("@costPrice", itemDto.CostPrice);
                    cmd.Parameters.AddWithValue("@lineTotal", lineTotal);

                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                _logger.LogInformation($"Item added to batch {batchId} with ID {itemId}");

                return (await GetBatchItemByIdAsync(itemId))!;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error adding item to batch {batchId}");
                throw;
            }
        }

        public async Task<bool> UpdateBatchItemAsync(int itemId, UpdatePurchaseBatchItemDto itemDto)
        {
            // Get current item to calculate stock difference
            var currentItem = await GetBatchItemByIdAsync(itemId);
            if (currentItem == null)
            {
                return false;
            }

            decimal quantityDifference = itemDto.QuantityReceived - currentItem.QuantityReceived;
            decimal newLineTotal = itemDto.LineTotal ?? (itemDto.QuantityReceived * itemDto.CostPrice);
            decimal oldLineTotal = currentItem.LineTotal;

            using var connection = _db.GetConnection();
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Update item
                string updateItem = @"
                    UPDATE purchase_batch_items 
                    SET quantity_recieved = @quantity,
                        cost_price = @costPrice,
                        CreatedAt = CreatedAt
                    WHERE purchase_batch_item_id = @itemId";

                using (var updateCmd = new MySqlCommand(updateItem, connection, transaction))
                {
                    updateCmd.Parameters.AddWithValue("@itemId", itemId);
                    updateCmd.Parameters.AddWithValue("@quantity", itemDto.QuantityReceived);
                    updateCmd.Parameters.AddWithValue("@costPrice", itemDto.CostPrice);
                    await updateCmd.ExecuteNonQueryAsync();
                }

                // Adjust stock if quantity changed
                if (quantityDifference != 0)
                {
                    string updateStock = @"
                        UPDATE product_variants 
                        SET quantity_in_stock = quantity_in_stock + @difference,
                            updated_at = CURRENT_TIMESTAMP
                        WHERE variant_id = @variantId";

                    using (var stockCmd = new MySqlCommand(updateStock, connection, transaction))
                    {
                        stockCmd.Parameters.AddWithValue("@variantId", currentItem.VariantId);
                        stockCmd.Parameters.AddWithValue("@difference", quantityDifference);
                        await stockCmd.ExecuteNonQueryAsync();
                    }
                }

                // Update batch total
                string updateBatch = @"
                    UPDATE purchase_batches
                    SET total_price = total_price - @oldTotal + @newTotal,
                        UpdatedAt = CURRENT_TIMESTAMP
                    WHERE batch_id = @batchId";

                using (var batchCmd = new MySqlCommand(updateBatch, connection, transaction))
                {
                    batchCmd.Parameters.AddWithValue("@batchId", currentItem.PurchaseBatchId);
                    batchCmd.Parameters.AddWithValue("@oldTotal", oldLineTotal);
                    batchCmd.Parameters.AddWithValue("@newTotal", newLineTotal);
                    await batchCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                _logger.LogInformation($"Batch item {itemId} updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error updating batch item {itemId}");
                throw;
            }
        }

        public async Task<bool> DeleteBatchItemAsync(int itemId)
        {
            // Get current item to reverse stock
            var item = await GetBatchItemByIdAsync(itemId);
            if (item == null)
            {
                return false;
            }

            using var connection = _db.GetConnection();
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Reverse stock
                string reverseStock = @"
                    UPDATE product_variants 
                    SET quantity_in_stock = quantity_in_stock - @quantity,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE variant_id = @variantId";

                using (var stockCmd = new MySqlCommand(reverseStock, connection, transaction))
                {
                    stockCmd.Parameters.AddWithValue("@variantId", item.VariantId);
                    stockCmd.Parameters.AddWithValue("@quantity", item.QuantityReceived);
                    await stockCmd.ExecuteNonQueryAsync();
                }

                // Update batch total
                string updateBatch = @"
                    UPDATE purchase_batches
                    SET total_price = total_price - @lineTotal,
                        UpdatedAt = CURRENT_TIMESTAMP
                    WHERE batch_id = @batchId";

                using (var batchCmd = new MySqlCommand(updateBatch, connection, transaction))
                {
                    batchCmd.Parameters.AddWithValue("@batchId", item.PurchaseBatchId);
                    batchCmd.Parameters.AddWithValue("@lineTotal", item.LineTotal);
                    await batchCmd.ExecuteNonQueryAsync();
                }

                // Delete item
                string deleteItem = "DELETE FROM purchase_batch_items WHERE purchase_batch_item_id = @itemId";
                using (var deleteCmd = new MySqlCommand(deleteItem, connection, transaction))
                {
                    deleteCmd.Parameters.AddWithValue("@itemId", itemId);
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                _logger.LogInformation($"Batch item {itemId} deleted successfully");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error deleting batch item {itemId}");
                throw;
            }
        }

        #endregion

        #region Search & Filters

        public async Task<List<PurchaseBatch>> SearchBatchesAsync(PurchaseBatchSearchDto searchDto)
        {
            var batches = new List<PurchaseBatch>();
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            string query = @"
                SELECT pb.*, s.name as supplier_name,
                       (pb.total_price - pb.paid) as remaining
                FROM purchase_batches pb
                LEFT JOIN supplier s ON pb.supplier_id = s.supplier_id
                WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
            {
                conditions.Add("(pb.BatchName LIKE @searchTerm OR s.name LIKE @searchTerm)");
                parameters.Add(new MySqlParameter("@searchTerm", $"%{searchDto.SearchTerm}%"));
            }

            if (searchDto.SupplierId.HasValue)
            {
                conditions.Add("pb.supplier_id = @supplierId");
                parameters.Add(new MySqlParameter("@supplierId", searchDto.SupplierId.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchDto.Status))
            {
                conditions.Add("pb.status = @status");
                parameters.Add(new MySqlParameter("@status", searchDto.Status));
            }

            if (searchDto.StartDate.HasValue)
            {
                conditions.Add("pb.CreatedAt >= @startDate");
                parameters.Add(new MySqlParameter("@startDate", searchDto.StartDate.Value));
            }

            if (searchDto.EndDate.HasValue)
            {
                conditions.Add("pb.CreatedAt <= @endDate");
                parameters.Add(new MySqlParameter("@endDate", searchDto.EndDate.Value));
            }

            string whereClause = conditions.Count > 0 ? " AND " + string.Join(" AND ", conditions) : "";
            query += whereClause + " ORDER BY pb.batch_id DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddRange(parameters.ToArray());

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    batches.Add(MapBatchFromReader(reader));
                }

                return batches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching batches");
                throw;
            }
        }

        public async Task<List<PurchaseBatchSummary>> GetBatchSummariesAsync()
        {
            var summaries = new List<PurchaseBatchSummary>();
            string query = @"
                SELECT 
                    pb.batch_id,
                    pb.BatchName,
                    s.name as supplier_name,
                    pb.CreatedAt,
                    COUNT(pbi.purchase_batch_item_id) as item_count,
                    pb.total_price,
                    pb.paid,
                    (pb.total_price - pb.paid) as remaining,
                    pb.status
                FROM purchase_batches pb
                LEFT JOIN supplier s ON pb.supplier_id = s.supplier_id
                LEFT JOIN purchase_batch_items pbi ON pb.batch_id = pbi.purchase_batch_id
                GROUP BY pb.batch_id
                ORDER BY pb.batch_id DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    summaries.Add(new PurchaseBatchSummary
                    {
                        BatchId = reader.GetInt32("batch_id"),
                        BatchName = reader.GetString("BatchName"),
                        SupplierName = reader.GetString("supplier_name"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        ItemCount = reader.GetInt32("item_count"),
                        TotalPrice = reader.GetDecimal("total_price"),
                        Paid = reader.GetDecimal("paid"),
                        Remaining = reader.GetDecimal("remaining"),
                        Status = reader.GetString("status")
                    });
                }

                return summaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch summaries");
                throw;
            }
        }

        public async Task<List<PurchaseBatch>> GetBatchesBySupplierAsync(int supplierId)
        {
            var batches = new List<PurchaseBatch>();
            string query = @"
                SELECT pb.*, s.name as supplier_name,
                       (pb.total_price - pb.paid) as remaining
                FROM purchase_batches pb
                LEFT JOIN supplier s ON pb.supplier_id = s.supplier_id
                WHERE pb.supplier_id = @supplierId
                ORDER BY pb.batch_id DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@supplierId", supplierId);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    batches.Add(MapBatchFromReader(reader));
                }

                return batches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving batches for supplier {supplierId}");
                throw;
            }
        }

        public async Task<List<PurchaseBatch>> GetBatchesByStatusAsync(string status)
        {
            var batches = new List<PurchaseBatch>();
            string query = @"
                SELECT pb.*, s.name as supplier_name,
                       (pb.total_price - pb.paid) as remaining
                FROM purchase_batches pb
                LEFT JOIN supplier s ON pb.supplier_id = s.supplier_id
                WHERE pb.status = @status
                ORDER BY pb.batch_id DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@status", status);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    batches.Add(MapBatchFromReader(reader));
                }

                return batches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving batches with status {status}");
                throw;
            }
        }

        #endregion

        #region Variant Selection (matching your UI)

        public async Task<List<VariantForSelectionDto>> GetVariantsForSelectionAsync(string? searchTerm = null)
        {
            var variants = new List<VariantForSelectionDto>();
            string query;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                query = @"
                    SELECT 
                        pv.variant_id,
                        p.name as product_name,
                        pv.size,
                        pv.class_type,
                        pv.price_per_unit as sale_price,
                        pv.quantity_in_stock
                    FROM product_variants pv
                    INNER JOIN products p ON pv.product_id = p.product_id
                    WHERE pv.is_active = TRUE AND p.is_active = TRUE
                    ORDER BY p.name, pv.size";
            }
            else
            {
                query = @"
                    SELECT 
                        pv.variant_id,
                        p.name as product_name,
                        pv.size,
                        pv.class_type,
                        pv.price_per_unit as sale_price,
                        pv.quantity_in_stock
                    FROM product_variants pv
                    INNER JOIN products p ON pv.product_id = p.product_id
                    WHERE (p.name LIKE @searchTerm OR pv.size LIKE @searchTerm OR pv.class_type LIKE @searchTerm)
                    AND pv.is_active = TRUE AND p.is_active = TRUE
                    ORDER BY p.name, pv.size";
            }

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    command.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    variants.Add(new VariantForSelectionDto
                    {
                        VariantId = reader.GetInt32("variant_id"),
                        ProductName = reader.GetString("product_name"),
                        Size = reader.IsDBNull(reader.GetOrdinal("size")) ? null : reader.GetString("size"),
                        ClassType = reader.IsDBNull(reader.GetOrdinal("class_type")) ? null : reader.GetString("class_type"),
                        SalePrice = reader.GetDecimal("sale_price"),
                        QuantityInStock = reader.GetDecimal("quantity_in_stock")
                    });
                }

                return variants;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving variants for selection");
                throw;
            }
        }

        #endregion

        #region Payment Management

        public async Task<bool> MakePaymentAsync(BatchPaymentDto paymentDto)
        {
            string query = @"
                UPDATE purchase_batches 
                SET paid = paid + @amount,
                    status = CASE 
                        WHEN (total_price - (paid + @amount)) <= 0 THEN 'Completed'
                        WHEN (paid + @amount) > 0 THEN 'Partial'
                        ELSE status
                    END,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE batch_id = @batchId";

            try
            {
                var parameters = new[]
                {
                    new MySqlParameter("@batchId", paymentDto.BatchId),
                    new MySqlParameter("@amount", paymentDto.Amount)
                };

                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Payment of {paymentDto.Amount} applied to batch {paymentDto.BatchId}");

                    // Record payment in a payments table if you have one
                    if (!string.IsNullOrWhiteSpace(paymentDto.Remarks))
                    {
                        // Optional: Insert into payments table
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error applying payment to batch {paymentDto.BatchId}");
                throw;
            }
        }

        public async Task<decimal> GetOutstandingBalanceAsync(int supplierId)
        {
            string query = @"
                SELECT COALESCE(SUM(total_price - paid), 0)
                FROM purchase_batches
                WHERE supplier_id = @supplierId AND total_price > paid";

            try
            {
                var parameters = new[] { new MySqlParameter("@supplierId", supplierId) };
                var result = await _db.ExecuteScalarAsync(query, parameters);
                return result != null ? Convert.ToDecimal(result) : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting outstanding balance for supplier {supplierId}");
                throw;
            }
        }

        #endregion

        #region Stock Management

        public async Task<bool> UpdateStockFromBatchAsync(int batchId)
        {
            string query = @"
                UPDATE product_variants pv
                INNER JOIN purchase_batch_items pbi ON pv.variant_id = pbi.variant_id
                SET pv.quantity_in_stock = pv.quantity_in_stock + pbi.quantity_recieved,
                    pv.updated_at = CURRENT_TIMESTAMP
                WHERE pbi.purchase_batch_id = @batchId";

            try
            {
                var parameters = new[] { new MySqlParameter("@batchId", batchId) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                _logger.LogInformation($"Stock updated from batch {batchId}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating stock from batch {batchId}");
                throw;
            }
        }

        public async Task<bool> ReverseStockFromBatchAsync(int batchId)
        {
            string query = @"
                UPDATE product_variants pv
                INNER JOIN purchase_batch_items pbi ON pv.variant_id = pbi.variant_id
                SET pv.quantity_in_stock = pv.quantity_in_stock - pbi.quantity_recieved,
                    pv.updated_at = CURRENT_TIMESTAMP
                WHERE pbi.purchase_batch_id = @batchId";

            try
            {
                var parameters = new[] { new MySqlParameter("@batchId", batchId) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                _logger.LogInformation($"Stock reversed from batch {batchId}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reversing stock from batch {batchId}");
                throw;
            }
        }

        #endregion

        #region Validation

        public async Task<bool> BatchNameExistsAsync(string batchName, int? excludeBatchId = null)
        {
            string query = excludeBatchId.HasValue
                ? "SELECT COUNT(*) FROM purchase_batches WHERE BatchName = @batchName AND batch_id != @excludeId"
                : "SELECT COUNT(*) FROM purchase_batches WHERE BatchName = @batchName";

            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter("@batchName", batchName)
            };

            if (excludeBatchId.HasValue)
            {
                parameters.Add(new MySqlParameter("@excludeId", excludeBatchId.Value));
            }

            var count = Convert.ToInt32(await _db.ExecuteScalarAsync(query, parameters.ToArray()));
            return count > 0;
        }

        #endregion

        #region Helper Methods

        private PurchaseBatch MapBatchFromReader(DbDataReader reader)
        {
            return new PurchaseBatch
            {
                BatchId = reader.GetInt32(reader.GetOrdinal("batch_id")),
                SupplierId = reader.GetInt32(reader.GetOrdinal("supplier_id")),
                BatchName = reader.GetString(reader.GetOrdinal("BatchName")),
                TotalPrice = reader.GetDecimal(reader.GetOrdinal("total_price")),
                Paid = reader.GetDecimal(reader.GetOrdinal("paid")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ?
                    reader.GetDateTime(reader.GetOrdinal("CreatedAt")) :
                    reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                SupplierName = reader.IsDBNull(reader.GetOrdinal("supplier_name")) ?
                    null : reader.GetString(reader.GetOrdinal("supplier_name"))
            };
        }

        private PurchaseBatchItem MapItemFromReader(DbDataReader reader)
        {
            return new PurchaseBatchItem
            {
                PurchaseBatchItemId = reader.GetInt32(reader.GetOrdinal("purchase_batch_item_id")),
                PurchaseBatchId = reader.GetInt32(reader.GetOrdinal("purchase_batch_id")),
                VariantId = reader.GetInt32(reader.GetOrdinal("variant_id")),
                QuantityReceived = reader.GetDecimal(reader.GetOrdinal("quantity_recieved")),
                CostPrice = reader.GetDecimal(reader.GetOrdinal("cost_price")),
                LineTotal = reader.GetDecimal(reader.GetOrdinal("line_total")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                ProductName = reader.GetString(reader.GetOrdinal("product_name")),
                Size = reader.IsDBNull(reader.GetOrdinal("size")) ? "Standard" : reader.GetString(reader.GetOrdinal("size")),
                ClassType = reader.IsDBNull(reader.GetOrdinal("class_type")) ? null : reader.GetString(reader.GetOrdinal("class_type")),
                SalePrice = reader.GetDecimal(reader.GetOrdinal("sale_price"))
            };
        }

        #endregion
    }
}