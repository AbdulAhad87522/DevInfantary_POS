using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;

namespace HardwareStoreAPI.Services
{
    public class SupplierBillService : ISupplierBillService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<SupplierBillService> _logger;

        public SupplierBillService(ILogger<SupplierBillService> logger)
        {
            _db = DatabaseHelper.Instance;
            _logger = logger;
        }

        public async Task<List<SupplierBillSummary>> GetAllSupplierBillSummariesAsync(string? search = null)
        {
            var summaries = new List<SupplierBillSummary>();

            string whereClause = string.IsNullOrWhiteSpace(search)
                ? ""
                : "WHERE s.name LIKE @search";

            string query = $@"
                SELECT 
                    s.supplier_id,
                    s.name as supplier_name,
                    COALESCE(SUM(pb.total_price), 0) as total_price,
                    COALESCE(SUM(pb.paid), 0) as paid,
                    COALESCE(SUM(pb.total_price - pb.paid), 0) as remaining,
                    COUNT(pb.batch_id) as batch_count,
                    CASE 
                        WHEN SUM(pb.paid) >= SUM(pb.total_price) THEN 'Completed'
                        WHEN SUM(pb.paid) > 0 THEN 'Partial'
                        ELSE 'Pending'
                    END as status
                FROM supplier s
                LEFT JOIN purchase_batches pb ON s.supplier_id = pb.supplier_id
                {whereClause}
                GROUP BY s.supplier_id, s.name
                HAVING SUM(pb.total_price) > 0
                ORDER BY s.name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);

                if (!string.IsNullOrWhiteSpace(search))
                    command.Parameters.AddWithValue("@search", $"%{search}%");

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    summaries.Add(new SupplierBillSummary
                    {
                        SupplierId = reader.GetInt32("supplier_id"),
                        SupplierName = reader.GetString("supplier_name"),
                        TotalPrice = reader.GetDecimal("total_price"),
                        Paid = reader.GetDecimal("paid"),
                        Remaining = reader.GetDecimal("remaining"),
                        Status = reader.GetString("status"),
                        BatchCount = reader.GetInt32("batch_count")
                    });
                }

                _logger.LogInformation($"Retrieved {summaries.Count} supplier bill summaries");
                return summaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier bill summaries");
                throw;
            }
        }

        public async Task<SupplierBillSummary?> GetSupplierBillSummaryAsync(int supplierId)
        {
            string query = @"
                SELECT 
                    s.supplier_id,
                    s.name as supplier_name,
                    COALESCE(SUM(pb.total_price), 0) as total_price,
                    COALESCE(SUM(pb.paid), 0) as paid,
                    COALESCE(SUM(pb.total_price - pb.paid), 0) as remaining,
                    COUNT(pb.batch_id) as batch_count,
                    CASE 
                        WHEN SUM(pb.paid) >= SUM(pb.total_price) THEN 'Completed'
                        WHEN SUM(pb.paid) > 0 THEN 'Partial'
                        ELSE 'Pending'
                    END as status
                FROM supplier s
                LEFT JOIN purchase_batches pb ON s.supplier_id = pb.supplier_id
                WHERE s.supplier_id = @supplierId
                GROUP BY s.supplier_id, s.name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@supplierId", supplierId);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new SupplierBillSummary
                    {
                        SupplierId = reader.GetInt32("supplier_id"),
                        SupplierName = reader.GetString("supplier_name"),
                        TotalPrice = reader.GetDecimal("total_price"),
                        Paid = reader.GetDecimal("paid"),
                        Remaining = reader.GetDecimal("remaining"),
                        Status = reader.GetString("status"),
                        BatchCount = reader.GetInt32("batch_count")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving bill summary for supplier {supplierId}");
                throw;
            }
        }

        public async Task<List<SupplierBatchDetail>> GetSupplierBatchesAsync(int supplierId)
        {
            var batches = new List<SupplierBatchDetail>();
            string query = @"
                SELECT 
                    pb.batch_id,
                    pb.supplier_id,
                    pb.BatchName,
                    pb.total_price,
                    pb.paid,
                    (pb.total_price - pb.paid) as remaining,
                    pb.status,
                    pb.created_at
                FROM purchase_batches pb
                WHERE pb.supplier_id = @supplierId
                ORDER BY pb.created_at DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@supplierId", supplierId);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    batches.Add(new SupplierBatchDetail
                    {
                        BatchId = reader.GetInt32("batch_id"),
                        SupplierId = reader.GetInt32("supplier_id"),
                        BatchName = reader.GetString("BatchName"),
                        TotalPrice = reader.GetDecimal("total_price"),
                        Paid = reader.GetDecimal("paid"),
                        Remaining = reader.GetDecimal("remaining"),
                        Status = reader.GetString("status"),
                        CreatedAt = reader.GetDateTime("created_at")
                    });
                }

                return batches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving batches for supplier {supplierId}");
                throw;
            }
        }

        public async Task<SupplierBatchDetail?> GetBatchDetailAsync(int batchId)
        {
            string batchQuery = @"
                SELECT 
                    pb.batch_id,
                    pb.supplier_id,
                    pb.BatchName,
                    pb.total_price,
                    pb.paid,
                    (pb.total_price - pb.paid) as remaining,
                    pb.status,
                    pb.created_at
                FROM purchase_batches pb
                WHERE pb.batch_id = @batchId";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(batchQuery, connection);
                command.Parameters.AddWithValue("@batchId", batchId);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var batch = new SupplierBatchDetail
                    {
                        BatchId = reader.GetInt32("batch_id"),
                        SupplierId = reader.GetInt32("supplier_id"),
                        BatchName = reader.GetString("BatchName"),
                        TotalPrice = reader.GetDecimal("total_price"),
                        Paid = reader.GetDecimal("paid"),
                        Remaining = reader.GetDecimal("remaining"),
                        Status = reader.GetString("status"),
                        CreatedAt = reader.GetDateTime("created_at")
                    };

                    await reader.CloseAsync();

                    // Get batch items
                    batch.Items = await GetBatchItemsAsync(batchId, connection);
                    return batch;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving batch detail for batch {batchId}");
                throw;
            }
        }

        public async Task<List<SupplierPaymentRecord>> GetSupplierPaymentRecordsAsync(int supplierId)
        {
            var records = new List<SupplierPaymentRecord>();
            string query = @"
                SELECT 
                    spr.*,
                    s.name as supplier_name,
                    pb.BatchName as batch_name
                FROM supplier_payment_records spr
                INNER JOIN supplier s ON spr.supplier_id = s.supplier_id
                INNER JOIN purchase_batches pb ON spr.batch_id = pb.batch_id
                WHERE spr.supplier_id = @supplierId
                ORDER BY spr.payment_date DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@supplierId", supplierId);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    records.Add(MapToPaymentRecord(reader));
                }

                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving payment records for supplier {supplierId}");
                throw;
            }
        }

        public async Task<List<SupplierPaymentRecord>> GetBatchPaymentRecordsAsync(int batchId)
        {
            var records = new List<SupplierPaymentRecord>();
            string query = @"
                SELECT 
                    spr.*,
                    s.name as supplier_name,
                    pb.BatchName as batch_name
                FROM supplier_payment_records spr
                INNER JOIN supplier s ON spr.supplier_id = s.supplier_id
                INNER JOIN purchase_batches pb ON spr.batch_id = pb.batch_id
                WHERE spr.batch_id = @batchId
                ORDER BY spr.payment_date DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@batchId", batchId);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    records.Add(MapToPaymentRecord(reader));
                }

                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving payment records for batch {batchId}");
                throw;
            }
        }

        public async Task<PaymentDistributionResult> AddSupplierPaymentAsync(AddSupplierPaymentDto paymentDto)
        {
            using var con = _db.GetConnection();
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                var result = new PaymentDistributionResult
                {
                    TotalPayment = paymentDto.PaymentAmount,
                    Applied = 0,
                    Remaining = paymentDto.PaymentAmount
                };

                // 1. Fetch unpaid batches (FIFO - First In First Out)
                string selectQuery = @"
                    SELECT batch_id, BatchName, total_price, paid, (total_price - paid) as due
                    FROM purchase_batches
                    WHERE supplier_id = @supplierId 
                    AND (total_price - paid) > 0
                    ORDER BY created_at ASC, batch_id ASC";

                var batches = new List<(int id, string name, decimal total, decimal paid, decimal due)>();
                using (var cmd = new MySqlCommand(selectQuery, con, (MySqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@supplierId", paymentDto.SupplierId);
                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        batches.Add((
                            reader.GetInt32("batch_id"),
                            reader.GetString("BatchName"),
                            reader.GetDecimal("total_price"),
                            reader.GetDecimal("paid"),
                            reader.GetDecimal("due")
                        ));
                    }
                }

                decimal remainingPayment = paymentDto.PaymentAmount;

                // 2. Distribute payment across batches
                foreach (var batch in batches)
                {
                    if (remainingPayment <= 0) break;

                    decimal toPay = Math.Min(remainingPayment, batch.due);

                    if (toPay > 0)
                    {
                        // Update batch paid amount and status
                        string updateBatchQuery = @"
                            UPDATE purchase_batches 
                            SET paid = paid + @toPay,
                                status = CASE 
                                    WHEN (paid + @toPay) >= total_price THEN 'Completed'
                                    WHEN (paid + @toPay) > 0 THEN 'Partial'
                                    ELSE 'Pending'
                                END
                            WHERE batch_id = @batch_id";

                        using (var updateCmd = new MySqlCommand(updateBatchQuery, con, (MySqlTransaction)tran))
                        {
                            updateCmd.Parameters.AddWithValue("@toPay", toPay);
                            updateCmd.Parameters.AddWithValue("@batch_id", batch.id);
                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        // Record payment in supplier_payment_records
                        string insertPaymentQuery = @"
                            INSERT INTO supplier_payment_records 
                            (supplier_id, batch_id, payment_amount, payment_date, remarks)
                            VALUES (@supplierId, @batchId, @amount, @date, @remarks)";

                        using (var cmdInsert = new MySqlCommand(insertPaymentQuery, con, (MySqlTransaction)tran))
                        {
                            cmdInsert.Parameters.AddWithValue("@supplierId", paymentDto.SupplierId);
                            cmdInsert.Parameters.AddWithValue("@batchId", batch.id);
                            cmdInsert.Parameters.AddWithValue("@amount", toPay);
                            cmdInsert.Parameters.AddWithValue("@date", paymentDto.PaymentDate);
                            cmdInsert.Parameters.AddWithValue("@remarks",
                                paymentDto.Remarks ?? $"Payment applied to batch {batch.name}");
                            await cmdInsert.ExecuteNonQueryAsync();
                        }

                        // Update supplier account balance
                        string updateSupplierQuery = @"
                            UPDATE supplier 
                            SET account_balance = account_balance - @amount,
                                updated_at = NOW()
                            WHERE supplier_id = @supplierId";

                        using (var supplierCmd = new MySqlCommand(updateSupplierQuery, con, (MySqlTransaction)tran))
                        {
                            supplierCmd.Parameters.AddWithValue("@amount", toPay);
                            supplierCmd.Parameters.AddWithValue("@supplierId", paymentDto.SupplierId);
                            await supplierCmd.ExecuteNonQueryAsync();
                        }

                        // Track allocation
                        result.Allocations.Add(new BatchPaymentAllocation
                        {
                            BatchId = batch.id,
                            BatchName = batch.name,
                            BatchDueBefore = batch.due,
                            PaymentApplied = toPay,
                            BatchDueAfter = batch.due - toPay
                        });

                        result.Applied += toPay;
                        remainingPayment -= toPay;
                    }
                }

                result.Remaining = remainingPayment;

                await tran.CommitAsync();
                _logger.LogInformation($"Payment of Rs. {paymentDto.PaymentAmount} added for supplier {paymentDto.SupplierId}");

                return result;
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                _logger.LogError(ex, "Error adding supplier payment");
                throw;
            }
        }

        private async Task<List<SupplierBatchItem>> GetBatchItemsAsync(int batchId, MySqlConnection connection)
        {
            var items = new List<SupplierBatchItem>();
            string query = @"
                SELECT 
                    pbi.*,
                    p.name as product_name,
                    pv.size,
                    pv.class_type,
                    pv.price_per_unit as sale_price
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
                items.Add(new SupplierBatchItem
                {
                    PurchaseBatchItemId = reader.GetInt32("purchase_batch_item_id"),
                    PurchaseBatchId = reader.GetInt32("purchase_batch_id"),
                    VariantId = reader.GetInt32("variant_id"),
                    ProductName = reader.GetString("product_name"),
                    Size = reader.IsDBNull(reader.GetOrdinal("size")) ? "Standard" : reader.GetString("size"),
                    ClassType = reader.IsDBNull(reader.GetOrdinal("class_type")) ? null : reader.GetString("class_type"),
                    QuantityReceived = reader.GetDecimal("quantity_recieved"),
                    CostPrice = reader.GetDecimal("cost_price"),
                    SalePrice = reader.GetDecimal("sale_price"),
                    LineTotal = reader.GetDecimal("quantity_recieved") * reader.GetDecimal("cost_price"),
                    CreatedAt = reader.GetDateTime("created_at")
                });
            }

            return items;
        }

        private SupplierPaymentRecord MapToPaymentRecord(DbDataReader reader)
        {
            return new SupplierPaymentRecord
            {
                PaymentId = reader.GetInt32("payment_id"),
                SupplierId = reader.GetInt32("supplier_id"),
                BatchId = reader.GetInt32("batch_id"),
                PaymentAmount = reader.GetDecimal("payment_amount"),
                PaymentDate = reader.GetDateTime("payment_date"),
                Remarks = reader.IsDBNull(reader.GetOrdinal("remarks"))
                    ? null
                    : reader.GetString("remarks"),
                CreatedAt = reader.GetDateTime("created_at"),
                SupplierName = reader.GetString("supplier_name"),
                BatchName = reader.GetString("batch_name")
            };
        }
    }
}