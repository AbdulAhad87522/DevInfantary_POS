using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;

namespace HardwareStoreAPI.Services
{
    public class PurchaseBatchService : IPurchaseBatchService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<PurchaseBatchService> _logger;

        public PurchaseBatchService(ILogger<PurchaseBatchService> logger)
        {
            _db = DatabaseHelper.Instance;
            _logger = logger;
        }

        public async Task<List<PurchaseBatch>> GetAllBatchesAsync()
        {
            var batches = new List<PurchaseBatch>();
            string query = @"
                SELECT 
                    pb.*,
                    s.name AS supplier_name,
                    (pb.total_price - pb.paid) AS remaining
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
                    batches.Add(MapToBatch(reader));
                }

                _logger.LogInformation($"Retrieved {batches.Count} purchase batches");
                return batches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all batches");
                throw;
            }
        }

        public async Task<PaginatedResponse<PurchaseBatch>> GetBatchesPaginatedAsync(int pageNumber, int pageSize, PurchaseBatchSearchDto? filters = null)
        {
            var response = new PaginatedResponse<PurchaseBatch>
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            try
            {
                var whereConditions = new List<string>();
                var parameters = new List<MySqlParameter>();

                if (filters != null)
                {
                    if (filters.StartDate.HasValue)
                    {
                        whereConditions.Add("pb.CreatedAt >= @startDate");
                        parameters.Add(new MySqlParameter("@startDate", filters.StartDate.Value));
                    }

                    if (filters.EndDate.HasValue)
                    {
                        whereConditions.Add("pb.CreatedAt <= @endDate");
                        parameters.Add(new MySqlParameter("@endDate", filters.EndDate.Value));
                    }

                    if (filters.SupplierId.HasValue)
                    {
                        whereConditions.Add("pb.supplier_id = @supplierId");
                        parameters.Add(new MySqlParameter("@supplierId", filters.SupplierId.Value));
                    }

                    if (!string.IsNullOrWhiteSpace(filters.BatchName))
                    {
                        whereConditions.Add("pb.BatchName LIKE @batchName");
                        parameters.Add(new MySqlParameter("@batchName", $"%{filters.BatchName}%"));
                    }

                    if (!string.IsNullOrWhiteSpace(filters.Status))
                    {
                        whereConditions.Add("pb.status = @status");
                        parameters.Add(new MySqlParameter("@status", filters.Status));
                    }
                }

                string whereClause = whereConditions.Count > 0
                    ? "WHERE " + string.Join(" AND ", whereConditions)
                    : "";

                // Get total count
                string countQuery = $@"
                    SELECT COUNT(*) 
                    FROM purchase_batches pb
                    {whereClause}";

                response.TotalRecords = Convert.ToInt32(
                    await _db.ExecuteScalarAsync(countQuery, parameters.ToArray()));

                // Get paginated data
                int offset = (pageNumber - 1) * pageSize;
                string query = $@"
                    SELECT 
                        pb.*,
                        s.name AS supplier_name,
                        (pb.total_price - pb.paid) AS remaining
                    FROM purchase_batches pb
                    LEFT JOIN supplier s ON pb.supplier_id = s.supplier_id
                    {whereClause}
                    ORDER BY pb.batch_id DESC
                    LIMIT @pageSize OFFSET @offset";

                parameters.Add(new MySqlParameter("@pageSize", pageSize));
                parameters.Add(new MySqlParameter("@offset", offset));

                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddRange(parameters.ToArray());

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    response.Data.Add(MapToBatch(reader));
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
                SELECT 
                    pb.*,
                    s.name AS supplier_name,
                    (pb.total_price - pb.paid) AS remaining
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
                    var batch = MapToBatch(reader);
                    var batchId = batch.BatchId;
                    await reader.CloseAsync();

                    // Load batch items
                    batch.Items = await GetBatchItemsAsync(batchId, connection);
                    return batch;
                }

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
            using var con = _db.GetConnection();
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                if (batchDto.Items == null || batchDto.Items.Count == 0)
                    throw new Exception("No items selected for purchase batch");

                // Get next batch ID
                string nextIdQuery = "SELECT COALESCE(MAX(batch_id), 0) + 1 FROM purchase_batches";
                int batchId;
                using (var idCmd = new MySqlCommand(nextIdQuery, con, (MySqlTransaction)tran))
                {
                    var result = await idCmd.ExecuteScalarAsync();
                    batchId = result != null ? Convert.ToInt32(result) : 1;
                }

                // Insert batch header
                string batchQuery = @"
            INSERT INTO purchase_batches 
            (batch_id, supplier_id, BatchName, created_at, total_price, paid, status)
            VALUES 
            (@batch_id, @supplier_id, @BatchName, @purchase_date, @total_price, @paid, @status)";

                using (var cmd = new MySqlCommand(batchQuery, con, (MySqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@batch_id", batchId);
                    cmd.Parameters.AddWithValue("@supplier_id", batchDto.SupplierId);
                    cmd.Parameters.AddWithValue("@BatchName", batchDto.BatchName);
                    cmd.Parameters.AddWithValue("@purchase_date", batchDto.PurchaseDate);
                    cmd.Parameters.AddWithValue("@total_price", batchDto.TotalPrice);
                    cmd.Parameters.AddWithValue("@paid", batchDto.Paid);
                    cmd.Parameters.AddWithValue("@status", batchDto.Status);
                    await cmd.ExecuteNonQueryAsync();
                }

                // ✅ Insert payment record if paid amount > 0
                if (batchDto.Paid > 0)
                {
                    string paymentQuery = @"
                INSERT INTO supplier_payment_records 
                (supplier_id, batch_id, payment_amount, payment_date, remarks, created_at)
                VALUES 
                (@supplier_id, @batch_id, @payment_amount, @payment_date, @remarks, NOW())";

                    using var paymentCmd = new MySqlCommand(paymentQuery, con, (MySqlTransaction)tran);
                    paymentCmd.Parameters.AddWithValue("@supplier_id", batchDto.SupplierId);
                    paymentCmd.Parameters.AddWithValue("@batch_id", batchId);
                    paymentCmd.Parameters.AddWithValue("@payment_amount", batchDto.Paid);
                    paymentCmd.Parameters.AddWithValue("@payment_date", batchDto.PurchaseDate);
                    paymentCmd.Parameters.AddWithValue("@remarks", $"Initial payment for batch: {batchDto.BatchName}");
                    await paymentCmd.ExecuteNonQueryAsync();

                    _logger.LogInformation($"Payment record created for batch {batchId}, amount: {batchDto.Paid}");
                }

                // Get next item ID
                string nextItemIdQuery = "SELECT COALESCE(MAX(purchase_batch_item_id), 0) + 1 FROM purchase_batch_items";
                int currentItemId;
                using (var itemIdCmd = new MySqlCommand(nextItemIdQuery, con, (MySqlTransaction)tran))
                {
                    var result = await itemIdCmd.ExecuteScalarAsync();
                    currentItemId = result != null ? Convert.ToInt32(result) : 1;
                }

                // Insert batch items and update stock
                foreach (var item in batchDto.Items)
                {
                    // Insert batch item
                    string itemQuery = @"
                INSERT INTO purchase_batch_items 
                (purchase_batch_item_id, purchase_batch_id, variant_id, quantity_recieved, cost_price)
                VALUES 
                (@item_id, @batch_id, @variant_id, @quantity, @cost_price)";

                    using var itemCmd = new MySqlCommand(itemQuery, con, (MySqlTransaction)tran);
                    itemCmd.Parameters.AddWithValue("@item_id", currentItemId);
                    itemCmd.Parameters.AddWithValue("@batch_id", batchId);
                    itemCmd.Parameters.AddWithValue("@variant_id", item.VariantId);
                    itemCmd.Parameters.AddWithValue("@quantity", item.QuantityReceived);
                    itemCmd.Parameters.AddWithValue("@cost_price", item.CostPrice);
                    await itemCmd.ExecuteNonQueryAsync();

                    // Update stock
                    string updateStockQuery = @"
                UPDATE product_variants 
                SET quantity_in_stock = quantity_in_stock + @quantity,
                    updated_at = NOW()
                WHERE variant_id = @variant_id";

                    using var stockCmd = new MySqlCommand(updateStockQuery, con, (MySqlTransaction)tran);
                    stockCmd.Parameters.AddWithValue("@quantity", item.QuantityReceived);
                    stockCmd.Parameters.AddWithValue("@variant_id", item.VariantId);
                    await stockCmd.ExecuteNonQueryAsync();

                    currentItemId++;
                }

                await tran.CommitAsync();
                _logger.LogInformation($"Purchase batch {batchId} created on {batchDto.PurchaseDate:yyyy-MM-dd}, stock updated");

                return (await GetBatchByIdAsync(batchId))!;
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                _logger.LogError(ex, "Error creating purchase batch");
                throw;
            }
        }

        public async Task<bool> UpdateBatchAsync(int id, UpdatePurchaseBatchDto batchDto)
        {
            string query = @"
                UPDATE purchase_batches 
                SET supplier_id = @supplier_id,
                    BatchName = @BatchName,
                    total_price = @total_price,
                    paid = @paid,
                    status = @status
                WHERE batch_id = @batch_id";

            try
            {
                var parameters = new[]
                {
                    new MySqlParameter("@batch_id", id),
                    new MySqlParameter("@supplier_id", batchDto.SupplierId),
                    new MySqlParameter("@BatchName", batchDto.BatchName),
                    new MySqlParameter("@total_price", batchDto.TotalPrice),
                    new MySqlParameter("@paid", batchDto.Paid),
                    new MySqlParameter("@status", batchDto.Status)
                };

                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Batch {id} updated successfully");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating batch {id}");
                throw;
            }
        }

        public async Task<bool> DeleteBatchAsync(int id)
        {
            using var con = _db.GetConnection();
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                // Get batch items to reverse stock
                var items = await GetBatchItemsAsync(id, con);

                // Reverse stock for each item
                foreach (var item in items)
                {
                    string reverseStockQuery = @"
                        UPDATE product_variants 
                        SET quantity_in_stock = quantity_in_stock - @quantity,
                            updated_at = NOW()
                        WHERE variant_id = @variant_id";

                    using var stockCmd = new MySqlCommand(reverseStockQuery, con, (MySqlTransaction)tran);
                    stockCmd.Parameters.AddWithValue("@quantity", item.QuantityReceived);
                    stockCmd.Parameters.AddWithValue("@variant_id", item.VariantId);
                    await stockCmd.ExecuteNonQueryAsync();
                }

                // Delete batch items
                string deleteItemsQuery = "DELETE FROM purchase_batch_items WHERE purchase_batch_id = @batch_id";
                using (var itemsCmd = new MySqlCommand(deleteItemsQuery, con, (MySqlTransaction)tran))
                {
                    itemsCmd.Parameters.AddWithValue("@batch_id", id);
                    await itemsCmd.ExecuteNonQueryAsync();
                }

                // Delete batch
                string deleteBatchQuery = "DELETE FROM purchase_batches WHERE batch_id = @batch_id";
                using (var batchCmd = new MySqlCommand(deleteBatchQuery, con, (MySqlTransaction)tran))
                {
                    batchCmd.Parameters.AddWithValue("@batch_id", id);
                    var result = await batchCmd.ExecuteNonQueryAsync();

                    if (result > 0)
                    {
                        await tran.CommitAsync();
                        _logger.LogInformation($"Batch {id} deleted, stock reversed");
                        return true;
                    }
                }

                await tran.RollbackAsync();
                return false;
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                _logger.LogError(ex, $"Error deleting batch {id}");
                throw;
            }
        }

        public async Task<List<PurchaseBatch>> SearchBatchesAsync(PurchaseBatchSearchDto searchDto)
        {
            var batches = new List<PurchaseBatch>();
            var whereConditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (searchDto.StartDate.HasValue)
            {
                whereConditions.Add("pb.CreatedAt >= @startDate");
                parameters.Add(new MySqlParameter("@startDate", searchDto.StartDate.Value));
            }

            if (searchDto.EndDate.HasValue)
            {
                whereConditions.Add("pb.CreatedAt <= @endDate");
                parameters.Add(new MySqlParameter("@endDate", searchDto.EndDate.Value));
            }

            if (searchDto.SupplierId.HasValue)
            {
                whereConditions.Add("pb.supplier_id = @supplierId");
                parameters.Add(new MySqlParameter("@supplierId", searchDto.SupplierId.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchDto.BatchName))
            {
                whereConditions.Add("(pb.BatchName LIKE @keyword OR s.name LIKE @keyword)");
                parameters.Add(new MySqlParameter("@keyword", $"%{searchDto.BatchName}%"));
            }

            if (!string.IsNullOrWhiteSpace(searchDto.Status))
            {
                whereConditions.Add("pb.status = @status");
                parameters.Add(new MySqlParameter("@status", searchDto.Status));
            }

            string whereClause = whereConditions.Count > 0
                ? "WHERE " + string.Join(" AND ", whereConditions)
                : "";

            string query = $@"
                SELECT 
                    pb.*,
                    s.name AS supplier_name,
                    (pb.total_price - pb.paid) AS remaining
                FROM purchase_batches pb
                LEFT JOIN supplier s ON pb.supplier_id = s.supplier_id
                {whereClause}
                ORDER BY pb.batch_id DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddRange(parameters.ToArray());
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    batches.Add(MapToBatch(reader));
                }

                return batches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching batches");
                throw;
            }
        }

        public async Task<List<PurchaseBatch>> GetBatchesBySupplierAsync(int supplierId)
        {
            var batches = new List<PurchaseBatch>();
            string query = @"
                SELECT 
                    pb.*,
                    s.name AS supplier_name,
                    (pb.total_price - pb.paid) AS remaining
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
                    batches.Add(MapToBatch(reader));
                }

                return batches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving batches for supplier {supplierId}");
                throw;
            }
        }

        public async Task<List<PurchaseBatch>> GetPendingBatchesAsync()
        {
            var batches = new List<PurchaseBatch>();
            string query = @"
                SELECT 
                    pb.*,
                    s.name AS supplier_name,
                    (pb.total_price - pb.paid) AS remaining
                FROM purchase_batches pb
                LEFT JOIN supplier s ON pb.supplier_id = s.supplier_id
                WHERE pb.status IN ('Pending', 'Partial')
                ORDER BY pb.batch_id DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    batches.Add(MapToBatch(reader));
                }

                return batches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending batches");
                throw;
            }
        }

        public async Task<List<ProductVariantForBatch>> GetProductVariantsForBatchAsync(string? searchTerm = null)
        {
            var variants = new List<ProductVariantForBatch>();

            string whereClause = string.IsNullOrWhiteSpace(searchTerm)
                ? ""
                : "AND (p.name LIKE @search OR pv.size LIKE @search OR pv.class_type LIKE @search)";

            string query = $@"
                SELECT 
                    pv.variant_id,
                    p.name AS product_name,
                    pv.size,
                    pv.class_type,
                    pv.price_per_unit AS sale_price,
                    pv.quantity_in_stock
                FROM product_variants pv
                INNER JOIN products p ON pv.product_id = p.product_id
                WHERE pv.is_active = TRUE AND p.is_active = TRUE
                {whereClause}
                ORDER BY p.name, pv.size";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                    command.Parameters.AddWithValue("@search", $"%{searchTerm}%");

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    variants.Add(new ProductVariantForBatch
                    {
                        VariantId = reader.GetInt32("variant_id"),
                        ProductName = reader.GetString("product_name"),
                        Size = reader.IsDBNull(reader.GetOrdinal("size")) ? "Standard" : reader.GetString("size"),
                        ClassType = reader.IsDBNull(reader.GetOrdinal("class_type")) ? null : reader.GetString("class_type"),
                        SalePrice = reader.GetDecimal("sale_price"),
                        QuantityInStock = reader.GetDecimal("quantity_in_stock")
                    });
                }

                return variants;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product variants for batch");
                throw;
            }
        }

        public async Task<int> GetNextBatchIdAsync()
        {
            string query = "SELECT COALESCE(MAX(batch_id), 0) + 1 FROM purchase_batches";

            try
            {
                var result = await _db.ExecuteScalarAsync(query);
                return result != null ? Convert.ToInt32(result) : 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next batch ID");
                throw;
            }
        }

        private async Task<List<PurchaseBatchItem>> GetBatchItemsAsync(int batchId, MySqlConnection connection)
        {
            var items = new List<PurchaseBatchItem>();
            string query = @"
                SELECT 
                    pbi.*,
                    p.name AS product_name,
                    pv.size,
                    pv.class_type,
                    pv.price_per_unit AS sale_price
                FROM purchase_batch_items pbi
                INNER JOIN product_variants pv ON pbi.variant_id = pv.variant_id
                INNER JOIN products p ON pv.product_id = p.product_id
                WHERE pbi.purchase_batch_id = @batchId
                ORDER BY pbi.purchase_batch_item_id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@batchId", batchId);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new PurchaseBatchItem
                {
                    PurchaseBatchItemId = reader.GetInt32("purchase_batch_item_id"),
                    PurchaseBatchId = reader.GetInt32("purchase_batch_id"),
                    VariantId = reader.GetInt32("variant_id"),
                    QuantityReceived = reader.GetDecimal("quantity_recieved"),
                    CostPrice = reader.GetDecimal("cost_price"),
                    LineTotal = reader.GetDecimal("quantity_recieved") * reader.GetDecimal("cost_price"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    ProductName = reader.GetString("product_name"),
                    Size = reader.IsDBNull(reader.GetOrdinal("size")) ? "Standard" : reader.GetString("size"),
                    ClassType = reader.IsDBNull(reader.GetOrdinal("class_type")) ? null : reader.GetString("class_type"),
                    SalePrice = reader.GetDecimal("sale_price")
                });
            }

            return items;
        }

        private PurchaseBatch MapToBatch(DbDataReader reader)
        {
            return new PurchaseBatch
            {
                BatchId = reader.GetInt32(reader.GetOrdinal("batch_id")),
                SupplierId = reader.GetInt32(reader.GetOrdinal("supplier_id")),
                BatchName = reader.GetString(reader.GetOrdinal("BatchName")),
                TotalPrice = reader.GetDecimal(reader.GetOrdinal("total_price")),
                Paid = reader.GetDecimal(reader.GetOrdinal("paid")),
                Remaining = reader.GetDecimal(reader.GetOrdinal("remaining")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                SupplierName = reader.IsDBNull(reader.GetOrdinal("supplier_name"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("supplier_name"))
            };
        }
    }
}