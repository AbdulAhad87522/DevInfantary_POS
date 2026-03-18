using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;

namespace HardwareStoreAPI.Services
{
    public class ReturnService : IReturnService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<ReturnService> _logger;

        public ReturnService(ILogger<ReturnService> logger)
        {
            _db = DatabaseHelper.Instance;
            _logger = logger;
        }

        public async Task<List<Return>> GetAllReturnsAsync()
        {
            var returns = new List<Return>();
            string query = @"
                SELECT 
                    r.*,
                    b.bill_number,
                    c.full_name AS customer_name,
                    l.value AS status
                FROM returns r
                LEFT JOIN bills b ON r.bill_id = b.bill_id
                LEFT JOIN customers c ON r.customer_id = c.customer_id
                LEFT JOIN lookup l ON r.status_id = l.lookup_id
                ORDER BY r.return_date DESC, r.return_id DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    returns.Add(MapToReturn(reader));
                }

                _logger.LogInformation($"Retrieved {returns.Count} returns");
                return returns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all returns");
                throw;
            }
        }

        public async Task<PaginatedResponse<Return>> GetReturnsPaginatedAsync(int pageNumber, int pageSize, ReturnSearchDto? filters = null)
        {
            var response = new PaginatedResponse<Return>
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
                        whereConditions.Add("r.return_date >= @startDate");
                        parameters.Add(new MySqlParameter("@startDate", filters.StartDate.Value));
                    }

                    if (filters.EndDate.HasValue)
                    {
                        whereConditions.Add("r.return_date <= @endDate");
                        parameters.Add(new MySqlParameter("@endDate", filters.EndDate.Value));
                    }

                    if (filters.CustomerId.HasValue)
                    {
                        whereConditions.Add("r.customer_id = @customerId");
                        parameters.Add(new MySqlParameter("@customerId", filters.CustomerId.Value));
                    }

                    if (filters.BillId.HasValue)
                    {
                        whereConditions.Add("r.bill_id = @billId");
                        parameters.Add(new MySqlParameter("@billId", filters.BillId.Value));
                    }

                    if (!string.IsNullOrWhiteSpace(filters.Status))
                    {
                        whereConditions.Add("l.value = @status");
                        parameters.Add(new MySqlParameter("@status", filters.Status));
                    }
                }

                string whereClause = whereConditions.Count > 0
                    ? "WHERE " + string.Join(" AND ", whereConditions)
                    : "";

                // Get total count
                string countQuery = $@"
                    SELECT COUNT(*) 
                    FROM returns r
                    LEFT JOIN lookup l ON r.status_id = l.lookup_id
                    {whereClause}";

                response.TotalRecords = Convert.ToInt32(
                    await _db.ExecuteScalarAsync(countQuery, parameters.ToArray()));

                // Get paginated data
                int offset = (pageNumber - 1) * pageSize;
                string query = $@"
                    SELECT 
                        r.*,
                        b.bill_number,
                        c.full_name AS customer_name,
                        l.value AS status
                    FROM returns r
                    LEFT JOIN bills b ON r.bill_id = b.bill_id
                    LEFT JOIN customers c ON r.customer_id = c.customer_id
                    LEFT JOIN lookup l ON r.status_id = l.lookup_id
                    {whereClause}
                    ORDER BY r.return_date DESC, r.return_id DESC
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
                    response.Data.Add(MapToReturn(reader));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated returns");
                throw;
            }
        }

        public async Task<Return?> GetReturnByIdAsync(int id)
        {
            string query = @"
                SELECT 
                    r.*,
                    b.bill_number,
                    c.full_name AS customer_name,
                    l.value AS status
                FROM returns r
                LEFT JOIN bills b ON r.bill_id = b.bill_id
                LEFT JOIN customers c ON r.customer_id = c.customer_id
                LEFT JOIN lookup l ON r.status_id = l.lookup_id
                WHERE r.return_id = @id";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var returnRecord = MapToReturn(reader);
                    var returnId = returnRecord.ReturnId;
                    await reader.CloseAsync();

                    // Load return items
                    returnRecord.Items = await GetReturnItemsAsync(returnId, connection);
                    return returnRecord;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving return with ID {id}");
                throw;
            }
        }

        public async Task<Return> ProcessReturnAsync(ProcessReturnDto returnDto)
        {
            using var con = _db.GetConnection();
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                if (returnDto.Items == null || returnDto.Items.Count == 0)
                    throw new Exception("No items selected for return");

                // Get Approved return_status
                int approvedStatusId;
                string statusQuery = "SELECT lookup_id FROM lookup WHERE type = 'return_status' AND value = 'Approved' LIMIT 1";
                using (var statusCmd = new MySqlCommand(statusQuery, con, (MySqlTransaction)tran))
                {
                    var statusResult = await statusCmd.ExecuteScalarAsync();
                    if (statusResult == null)
                        throw new Exception("Return status 'Approved' not found in lookup table");
                    approvedStatusId = Convert.ToInt32(statusResult);
                }

                // Get customer_id from bill
                int? customerId = null;
                string getCustomerQuery = "SELECT customer_id FROM bills WHERE bill_id = @billId LIMIT 1";
                using (var customerCmd = new MySqlCommand(getCustomerQuery, con, (MySqlTransaction)tran))
                {
                    customerCmd.Parameters.AddWithValue("@billId", returnDto.BillId);
                    var result = await customerCmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                        customerId = Convert.ToInt32(result);
                }

                // Insert return header
                string insertReturnQuery = @"
                    INSERT INTO returns 
                    (bill_id, customer_id, refund_amount, status_id, reason, notes, return_date)
                    VALUES 
                    (@billId, @customerId, @refundAmount, @statusId, @reason, @notes, NOW());
                    SELECT LAST_INSERT_ID();";

                int returnId;
                using (var cmd = new MySqlCommand(insertReturnQuery, con, (MySqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@billId", returnDto.BillId);
                    cmd.Parameters.AddWithValue("@customerId", customerId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@refundAmount", returnDto.RefundAmount);
                    cmd.Parameters.AddWithValue("@statusId", approvedStatusId);
                    cmd.Parameters.AddWithValue("@reason", returnDto.Reason);
                    cmd.Parameters.AddWithValue("@notes", returnDto.Notes ?? (object)DBNull.Value);

                    var result = await cmd.ExecuteScalarAsync();
                    if (result == null)
                        throw new Exception("Failed to create return record");
                    returnId = Convert.ToInt32(result);
                }

                // Insert return items + optionally restore stock
                foreach (var item in returnDto.Items)
                {
                    string conditionNote = returnDto.RestoreStock
                        ? "Resalable"
                        : "Damaged - not restocked";

                    // Insert return item
                    string insertItemQuery = @"
                        INSERT INTO return_items (return_id, variant_id, quantity, condition_note)
                        VALUES (@returnId, @variantId, @quantity, @conditionNote)";

                    using var itemCmd = new MySqlCommand(insertItemQuery, con, (MySqlTransaction)tran);
                    itemCmd.Parameters.AddWithValue("@returnId", returnId);
                    itemCmd.Parameters.AddWithValue("@variantId", item.VariantId);
                    itemCmd.Parameters.AddWithValue("@quantity", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@conditionNote", conditionNote);
                    await itemCmd.ExecuteNonQueryAsync();

                    // Restore stock only if user opted in
                    if (returnDto.RestoreStock)
                    {
                        string restockQuery = @"
                            UPDATE product_variants
                            SET quantity_in_stock = quantity_in_stock + @qty,
                                updated_at = NOW()
                            WHERE variant_id = @variantId";

                        using var restockCmd = new MySqlCommand(restockQuery, con, (MySqlTransaction)tran);
                        restockCmd.Parameters.AddWithValue("@qty", item.Quantity);
                        restockCmd.Parameters.AddWithValue("@variantId", item.VariantId);
                        await restockCmd.ExecuteNonQueryAsync();
                    }
                }

                // Reduce customer balance (credit customers only)
                if (customerId.HasValue)
                {
                    string updateBalanceQuery = @"
                        UPDATE customers
                        SET current_balance = GREATEST(0, current_balance - @refundAmount),
                            updated_at = NOW()
                        WHERE customer_id = @customerId
                        AND current_balance > 0";

                    using var balanceCmd = new MySqlCommand(updateBalanceQuery, con, (MySqlTransaction)tran);
                    balanceCmd.Parameters.AddWithValue("@refundAmount", returnDto.RefundAmount);
                    balanceCmd.Parameters.AddWithValue("@customerId", customerId.Value);
                    await balanceCmd.ExecuteNonQueryAsync();
                }

                // Mark bill as Refunded
                string refundedStatusQuery = "SELECT lookup_id FROM lookup WHERE type = 'payment_status' AND value = 'Refunded' LIMIT 1";
                using (var refundedCmd = new MySqlCommand(refundedStatusQuery, con, (MySqlTransaction)tran))
                {
                    var refundedResult = await refundedCmd.ExecuteScalarAsync();
                    if (refundedResult != null)
                    {
                        int refundedStatusId = Convert.ToInt32(refundedResult);

                        string updateBillQuery = @"
                            UPDATE bills
                            SET payment_status_id = @statusId,
                                updated_at = NOW()
                            WHERE bill_id = @billId";

                        using var billCmd = new MySqlCommand(updateBillQuery, con, (MySqlTransaction)tran);
                        billCmd.Parameters.AddWithValue("@statusId", refundedStatusId);
                        billCmd.Parameters.AddWithValue("@billId", returnDto.BillId);
                        await billCmd.ExecuteNonQueryAsync();
                    }
                }

                await tran.CommitAsync();
                _logger.LogInformation($"Return processed with ID {returnId}");

                return (await GetReturnByIdAsync(returnId))!;
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                _logger.LogError(ex, "Error processing return");
                throw;
            }
        }

        public async Task<List<Return>> SearchReturnsAsync(ReturnSearchDto searchDto)
        {
            var returns = new List<Return>();
            var whereConditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (searchDto.StartDate.HasValue)
            {
                whereConditions.Add("r.return_date >= @startDate");
                parameters.Add(new MySqlParameter("@startDate", searchDto.StartDate.Value));
            }

            if (searchDto.EndDate.HasValue)
            {
                whereConditions.Add("r.return_date <= @endDate");
                parameters.Add(new MySqlParameter("@endDate", searchDto.EndDate.Value));
            }

            if (searchDto.CustomerId.HasValue)
            {
                whereConditions.Add("r.customer_id = @customerId");
                parameters.Add(new MySqlParameter("@customerId", searchDto.CustomerId.Value));
            }

            if (searchDto.BillId.HasValue)
            {
                whereConditions.Add("r.bill_id = @billId");
                parameters.Add(new MySqlParameter("@billId", searchDto.BillId.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchDto.Status))
            {
                whereConditions.Add("l.value = @status");
                parameters.Add(new MySqlParameter("@status", searchDto.Status));
            }

            string whereClause = whereConditions.Count > 0
                ? "WHERE " + string.Join(" AND ", whereConditions)
                : "";

            string query = $@"
                SELECT 
                    r.*,
                    b.bill_number,
                    c.full_name AS customer_name,
                    l.value AS status
                FROM returns r
                LEFT JOIN bills b ON r.bill_id = b.bill_id
                LEFT JOIN customers c ON r.customer_id = c.customer_id
                LEFT JOIN lookup l ON r.status_id = l.lookup_id
                {whereClause}
                ORDER BY r.return_date DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddRange(parameters.ToArray());
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    returns.Add(MapToReturn(reader));
                }

                return returns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching returns");
                throw;
            }
        }

        public async Task<List<Return>> GetReturnsByCustomerAsync(int customerId)
        {
            var returns = new List<Return>();
            string query = @"
                SELECT 
                    r.*,
                    b.bill_number,
                    c.full_name AS customer_name,
                    l.value AS status
                FROM returns r
                LEFT JOIN bills b ON r.bill_id = b.bill_id
                LEFT JOIN customers c ON r.customer_id = c.customer_id
                LEFT JOIN lookup l ON r.status_id = l.lookup_id
                WHERE r.customer_id = @customerId
                ORDER BY r.return_date DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@customerId", customerId);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    returns.Add(MapToReturn(reader));
                }

                return returns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving returns for customer {customerId}");
                throw;
            }
        }

        public async Task<List<Return>> GetReturnsByBillAsync(int billId)
        {
            var returns = new List<Return>();
            string query = @"
                SELECT 
                    r.*,
                    b.bill_number,
                    c.full_name AS customer_name,
                    l.value AS status
                FROM returns r
                LEFT JOIN bills b ON r.bill_id = b.bill_id
                LEFT JOIN customers c ON r.customer_id = c.customer_id
                LEFT JOIN lookup l ON r.status_id = l.lookup_id
                WHERE r.bill_id = @billId
                ORDER BY r.return_date DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@billId", billId);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    returns.Add(MapToReturn(reader));
                }

                return returns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving returns for bill {billId}");
                throw;
            }
        }

        public async Task<BillForReturnDto?> GetBillForReturnAsync(string billNumber)
        {
            string billQuery = @"
                SELECT
                    b.bill_id,
                    b.bill_number,
                    COALESCE(c.full_name, 'Walk-in Customer') AS customer_name,
                    b.discount_percentage,
                    b.bill_date,
                    b.total_amount
                FROM bills b
                LEFT JOIN customers c ON c.customer_id = b.customer_id
                WHERE b.bill_number = @billNumber
                LIMIT 1";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(billQuery, connection);
                command.Parameters.AddWithValue("@billNumber", billNumber);
                using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return null;

                var bill = new BillForReturnDto
                {
                    BillId = reader.GetInt32("bill_id"),
                    discount_percentage = reader.GetInt32("discount_percentage"),
                    BillNumber = reader.GetString("bill_number"),
                    CustomerName = reader.GetString("customer_name"),
                    BillDate = reader.GetDateTime("bill_date"),
                    TotalAmount = reader.GetDecimal("total_amount")
                };

                var billId = bill.BillId;
                await reader.CloseAsync();

                // Get bill items
                string itemsQuery = @"
                    SELECT
                        bi.bill_item_id,
                        bi.bill_id,
                        bi.product_id,
                        bi.variant_id,
                        p.name AS product_name,
                        pv.size,
                        bi.unit_of_measure,
                        bi.quantity,
                        bi.unit_price,
                        bi.line_total,
                        bi.notes
                    FROM bill_items bi
                    JOIN products p ON p.product_id = bi.product_id
                    JOIN product_variants pv ON pv.variant_id = bi.variant_id
                    WHERE bi.bill_id = @billId
                    ORDER BY bi.bill_item_id";

                using var itemsCommand = new MySqlCommand(itemsQuery, connection);
                itemsCommand.Parameters.AddWithValue("@billId", billId);
                using var itemsReader = await itemsCommand.ExecuteReaderAsync();

                while (await itemsReader.ReadAsync())
                {
                    bill.Items.Add(new BillItemForReturnDto
                    {
                        BillItemId = itemsReader.GetInt32("bill_item_id"),
                        BillId = itemsReader.GetInt32("bill_id"),
                        ProductId = itemsReader.GetInt32("product_id"),
                        VariantId = itemsReader.GetInt32("variant_id"),
                        ProductName = itemsReader.GetString("product_name"),
                        Size = itemsReader.GetString("size"),
                        UnitOfMeasure = itemsReader.GetString("unit_of_measure"),
                        Quantity = itemsReader.GetDecimal("quantity"),
                        UnitPrice = itemsReader.GetDecimal("unit_price"),
                        LineTotal = itemsReader.GetDecimal("line_total"),
                        Notes = itemsReader.IsDBNull(itemsReader.GetOrdinal("notes"))
                            ? null
                            : itemsReader.GetString("notes")
                    });
                }

                return bill;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving bill for return: {billNumber}");
                throw;
            }
        }

        private async Task<List<ReturnItem>> GetReturnItemsAsync(int returnId, MySqlConnection connection)
        {
            var items = new List<ReturnItem>();
            string query = @"
                SELECT 
                    ri.*,
                    p.name AS product_name,
                    pv.size,
                    pv.unit_of_measure
                FROM return_items ri
                INNER JOIN product_variants pv ON ri.variant_id = pv.variant_id
                INNER JOIN products p ON pv.product_id = p.product_id
                WHERE ri.return_id = @returnId
                ORDER BY ri.return_item_id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@returnId", returnId);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new ReturnItem
                {
                    ReturnItemId = reader.GetInt32("return_item_id"),
                    ReturnId = reader.GetInt32("return_id"),
                    VariantId = reader.GetInt32("variant_id"),
                    Quantity = reader.GetDecimal("quantity"),
                    ConditionNote = reader.IsDBNull(reader.GetOrdinal("condition_note"))
                        ? null
                        : reader.GetString("condition_note"),
                    ProductName = reader.GetString("product_name"),
                    Size = reader.GetString("size"),
                    UnitOfMeasure = reader.GetString("unit_of_measure")
                });
            }

            return items;
        }

        private Return MapToReturn(DbDataReader reader)
        {
            return new Return
            {
                ReturnId = reader.GetInt32(reader.GetOrdinal("return_id")),
                BillId = reader.IsDBNull(reader.GetOrdinal("bill_id"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("bill_id")),
                CustomerId = reader.IsDBNull(reader.GetOrdinal("customer_id"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("customer_id")),
                ReturnDate = reader.GetDateTime(reader.GetOrdinal("return_date")),
                RefundAmount = reader.GetDecimal(reader.GetOrdinal("refund_amount")),
                StatusId = reader.GetInt32(reader.GetOrdinal("status_id")),
                Reason = reader.IsDBNull(reader.GetOrdinal("reason"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("reason")),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("notes")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                BillNumber = reader.IsDBNull(reader.GetOrdinal("bill_number"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("bill_number")),
                CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_name"))
                    ? "Walk-in Customer"
                    : reader.GetString(reader.GetOrdinal("customer_name")),
                Status = reader.GetString(reader.GetOrdinal("status"))
            };
        }
    }
}