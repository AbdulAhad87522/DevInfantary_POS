using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace HardwareStoreAPI.Services
{
    public class BillService : IBillService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<BillService> _logger;
        private readonly IPdfService _pdfService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BillService(ILogger<BillService> logger, IPdfService pdfService, IHttpContextAccessor httpContextAccessor)
        {
            _db = DatabaseHelper.Instance;
            _logger = logger;
            _pdfService = pdfService;
            _httpContextAccessor = httpContextAccessor;
        }

        private int GetCurrentStaffId()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("User not authenticated, defaulting to staff_id = 1");
                return 1; // Default to 1 if not authenticated
            }

            // Try multiple claim types for robustness
            var staffIdClaim = user.FindFirst("StaffId")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (int.TryParse(staffIdClaim, out int staffId))
            {
                _logger.LogInformation($"Extracted staff ID from token: {staffId}");
                return staffId;
            }

            _logger.LogWarning("Could not parse staff ID from token, defaulting to staff_id = 1");
            return 1; // Fallback to 1
        }


        public async Task<List<Bill>> GetAllBillsAsync()
        {
            var bills = new List<Bill>();
            string query = @"
                SELECT 
                    b.*,
                    c.full_name AS customer_name,
                    l.value AS payment_status
                FROM bills b
                LEFT JOIN customers c ON b.customer_id = c.customer_id
                LEFT JOIN lookup l ON b.payment_status_id = l.lookup_id
                ORDER BY b.bill_date DESC, b.bill_id DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    bills.Add(MapToBill(reader));
                }

                _logger.LogInformation($"Retrieved {bills.Count} bills");
                return bills;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all bills");
                throw;
            }
        }

        public async Task<PaginatedResponse<Bill>> GetBillsPaginatedAsync(int pageNumber, int pageSize, BillSearchDto? filters = null)
        {
            var response = new PaginatedResponse<Bill>
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
                        whereConditions.Add("b.bill_date >= @startDate");
                        parameters.Add(new MySqlParameter("@startDate", filters.StartDate.Value));
                    }

                    if (filters.EndDate.HasValue)
                    {
                        whereConditions.Add("b.bill_date <= @endDate");
                        parameters.Add(new MySqlParameter("@endDate", filters.EndDate.Value));
                    }

                    if (filters.CustomerId.HasValue)
                    {
                        whereConditions.Add("b.customer_id = @customerId");
                        parameters.Add(new MySqlParameter("@customerId", filters.CustomerId.Value));
                    }

                    if (!string.IsNullOrWhiteSpace(filters.BillNumber))
                    {
                        whereConditions.Add("b.bill_number LIKE @billNumber");
                        parameters.Add(new MySqlParameter("@billNumber", $"%{filters.BillNumber}%"));
                    }

                    if (!string.IsNullOrWhiteSpace(filters.PaymentStatus))
                    {
                        whereConditions.Add("l.value = @paymentStatus");
                        parameters.Add(new MySqlParameter("@paymentStatus", filters.PaymentStatus));
                    }
                }

                string whereClause = whereConditions.Count > 0
                    ? "WHERE " + string.Join(" AND ", whereConditions)
                    : "";

                // Get total count
                string countQuery = $@"
                    SELECT COUNT(*) 
                    FROM bills b
                    LEFT JOIN lookup l ON b.payment_status_id = l.lookup_id
                    {whereClause}";

                response.TotalRecords = Convert.ToInt32(
                    await _db.ExecuteScalarAsync(countQuery, parameters.ToArray()));

                // Get paginated data
                int offset = (pageNumber - 1) * pageSize;
                string query = $@"
                    SELECT 
                        b.*,
                        c.full_name AS customer_name,
                        l.value AS payment_status
                    FROM bills b
                    LEFT JOIN customers c ON b.customer_id = c.customer_id
                    LEFT JOIN lookup l ON b.payment_status_id = l.lookup_id
                    {whereClause}
                    ORDER BY b.bill_date DESC, b.bill_id DESC
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
                    response.Data.Add(MapToBill(reader));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated bills");
                throw;
            }
        }

        public async Task<Bill?> GetBillByIdAsync(int id)
        {
            string query = @"
                SELECT 
                    b.*,
                    c.full_name AS customer_name,
                    l.value AS payment_status
                FROM bills b
                LEFT JOIN customers c ON b.customer_id = c.customer_id
                LEFT JOIN lookup l ON b.payment_status_id = l.lookup_id
                WHERE b.bill_id = @id";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var bill = MapToBill(reader);
                    reader.Close();

                    // Load bill items
                    bill.Items = await GetBillItemsAsync(id, connection);
                    return bill;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving bill with ID {id}");
                throw;
            }
        }

        public async Task<Bill?> GetBillByNumberAsync(string billNumber)
        {
            string query = @"
                SELECT 
                    b.*,
                    c.full_name AS customer_name,
                    l.value AS payment_status
                FROM bills b
                LEFT JOIN customers c ON b.customer_id = c.customer_id
                LEFT JOIN lookup l ON b.payment_status_id = l.lookup_id
                WHERE b.bill_number = @billNumber";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@billNumber", billNumber);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var bill = MapToBill(reader);
                    var billId = bill.BillId;
                    reader.Close();

                    bill.Items = await GetBillItemsAsync(billId, connection);
                    return bill;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving bill with number {billNumber}");
                throw;
            }
        }

        public async Task<BillWithPdfResponse> CreateBillAsync(CreateBillDto billDto)
        {
            // ✅ Get the actual logged-in staff ID from HttpContext
            int staffId = GetCurrentStaffId();

            // ✅ Log the staff ID being used
            _logger.LogInformation($"Creating bill with staff ID: {staffId}");

            using var con = _db.GetConnection();
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                if (billDto.Items == null || billDto.Items.Count == 0)
                    throw new Exception("No products selected for sale");

                // ✅ Convert NULL customer_id to 1 (Walk-in Customer)
                if (!billDto.CustomerId.HasValue || billDto.CustomerId.Value == 0)
                {
                    billDto.CustomerId = 1;
                    _logger.LogInformation("Customer ID was null/0, set to 1 (Walk-in Customer)");
                }

                // Generate bill number
                string billNumber = $"INV-{DateTime.Now:yyyy}-{DateTime.Now:MMddHHmmss}";

                // Get payment status
                int paymentStatusId;
                string statusQuery = "SELECT lookup_id FROM lookup WHERE type = 'payment_status' AND value = @status";
                using (var statusCmd = new MySqlCommand(statusQuery, con, (MySqlTransaction)tran))
                {
                    string status = (billDto.PaidAmount >= billDto.TotalAmount) ? "Paid" : "Partial";
                    statusCmd.Parameters.AddWithValue("@status", status);
                    var statusResult = await statusCmd.ExecuteScalarAsync();
                    if (statusResult == null)
                        throw new Exception("Payment status not found in lookup table");
                    paymentStatusId = Convert.ToInt32(statusResult);
                }

                // Insert bill
                decimal amountDue = billDto.TotalAmount - billDto.PaidAmount;
                string billQuery = @"
            INSERT INTO bills 
            (bill_number, bill_date, customer_id, staff_id, subtotal, discount_amount, 
             total_amount, amount_paid, amount_due, payment_status_id) 
            VALUES 
            (@bill_number, @bill_date, @customer_id, @staff_id, @subtotal, @discount_amount,
             @total_amount, @amount_paid, @amount_due, @payment_status_id);
            SELECT LAST_INSERT_ID();";

                int billId;
                using (var cmd = new MySqlCommand(billQuery, con, (MySqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@bill_number", billNumber);
                    cmd.Parameters.AddWithValue("@bill_date", billDto.BillDate);
                    cmd.Parameters.AddWithValue("@customer_id", billDto.CustomerId.Value);
                    cmd.Parameters.AddWithValue("@staff_id", staffId);  // ✅ Uses logged-in staff ID
                    cmd.Parameters.AddWithValue("@subtotal", billDto.TotalAmount);
                    cmd.Parameters.AddWithValue("@discount_amount", billDto.DiscountAmount);
                    cmd.Parameters.AddWithValue("@total_amount", billDto.TotalAmount);
                    cmd.Parameters.AddWithValue("@amount_paid", billDto.PaidAmount);
                    cmd.Parameters.AddWithValue("@amount_due", amountDue);
                    cmd.Parameters.AddWithValue("@payment_status_id", paymentStatusId);

                    var result = await cmd.ExecuteScalarAsync();
                    if (result == null)
                        throw new Exception("Failed to get bill ID");
                    billId = Convert.ToInt32(result);
                }

                // ✅ Store items for PDF generation
                var pdfItems = new List<BillItemPdfData>();

                // Process each item
                foreach (var item in billDto.Items)
                {
                    if (string.IsNullOrEmpty(item.ProductName))
                        throw new Exception("Product name is missing");

                    if (item.Quantity <= 0)
                        throw new Exception($"Invalid quantity for product: {item.ProductName}");

                    string productQuery = @"
                SELECT 
                    p.product_id, 
                    pv.variant_id,
                    pv.size,
                    pv.class_type,
                    pv.price_per_unit,
                    pv.quantity_in_stock,
                    pv.unit_of_measure
                FROM products p
                INNER JOIN product_variants pv ON p.product_id = pv.product_id
                WHERE p.name = @ProductName 
                AND (pv.size = @size OR (pv.size IS NULL AND @size = ''))
                AND p.is_active = TRUE 
                AND pv.is_active = TRUE
                LIMIT 1";

                    using var productCmd = new MySqlCommand(productQuery, con, (MySqlTransaction)tran);
                    productCmd.Parameters.AddWithValue("@ProductName", item.ProductName);
                    productCmd.Parameters.AddWithValue("@size", item.Size ?? "");
                    productCmd.Parameters.AddWithValue("@class_type", item.ClassType ?? "");

                    using var reader = await productCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        int productId = reader.GetInt32("product_id");
                        int variantId = reader.GetInt32("variant_id");
                        string size = reader.IsDBNull(reader.GetOrdinal("size")) ? null : reader.GetString("size");
                        string classType = reader.IsDBNull(reader.GetOrdinal("class_type")) ? null : reader.GetString("class_type");
                        decimal salePrice = reader.GetDecimal("price_per_unit");
                        decimal currentStock = reader.GetDecimal("quantity_in_stock");
                        string unitOfMeasure = reader.GetString("unit_of_measure");
                        await reader.CloseAsync();

                        if (currentStock < item.Quantity)
                        {
                            string variantDesc = $"{item.ProductName} ({size ?? "N/A"} - {classType ?? "N/A"})";
                            throw new Exception($"Insufficient stock for {variantDesc}. Available: {currentStock}, Requested: {item.Quantity}");
                        }

                        decimal unitPrice = item.UnitPrice ?? salePrice;
                        decimal lineTotal = unitPrice * item.Quantity;

                        // ✅ Add to PDF items list
                        pdfItems.Add(new BillItemPdfData
                        {
                            ProductName = item.ProductName,
                            Size = size,
                            Quantity = item.Quantity,
                            UnitOfMeasure = unitOfMeasure,
                            UnitPrice = unitPrice,
                            LineTotal = lineTotal
                        });

                        // Insert bill item
                        string billItemQuery = @"
                    INSERT INTO bill_items 
                    (bill_id, product_id, variant_id, quantity, unit_of_measure, unit_price, line_total, notes) 
                    VALUES 
                    (@bill_id, @product_id, @variant_id, @quantity, @unit_of_measure, @unit_price, @line_total, @notes)";

                        using var billItemCmd = new MySqlCommand(billItemQuery, con, (MySqlTransaction)tran);
                        billItemCmd.Parameters.AddWithValue("@bill_id", billId);
                        billItemCmd.Parameters.AddWithValue("@product_id", productId);
                        billItemCmd.Parameters.AddWithValue("@variant_id", variantId);
                        billItemCmd.Parameters.AddWithValue("@quantity", item.Quantity);
                        billItemCmd.Parameters.AddWithValue("@unit_of_measure", unitOfMeasure);
                        billItemCmd.Parameters.AddWithValue("@unit_price", unitPrice);
                        billItemCmd.Parameters.AddWithValue("@line_total", lineTotal);
                        billItemCmd.Parameters.AddWithValue("@notes", DBNull.Value);

                        int rowsInserted = await billItemCmd.ExecuteNonQueryAsync();
                        if (rowsInserted == 0)
                            throw new Exception($"Failed to insert bill item for: {item.ProductName}");

                        // Deduct stock
                        string updateStockQuery = @"
                    UPDATE product_variants 
                    SET quantity_in_stock = quantity_in_stock - @quantity,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE variant_id = @variant_id";

                        using var stockCmd = new MySqlCommand(updateStockQuery, con, (MySqlTransaction)tran);
                        stockCmd.Parameters.AddWithValue("@quantity", item.Quantity);
                        stockCmd.Parameters.AddWithValue("@variant_id", variantId);

                        int stockUpdated = await stockCmd.ExecuteNonQueryAsync();
                        if (stockUpdated == 0)
                            throw new Exception($"Failed to update stock for: {item.ProductName}");

                        string variantInfo = $"{item.ProductName} (Size: {size ?? "N/A"}, Type: {classType ?? "N/A"})";
                        _logger.LogInformation($"Stock updated for variant {variantId} [{variantInfo}]: -{item.Quantity} {unitOfMeasure}");
                    }
                    else
                    {
                        await reader.CloseAsync();
                        string variantDesc = $"{item.ProductName} (Size: {item.Size ?? "N/A"}, Type: {item.ClassType ?? "N/A"})";
                        throw new Exception($"Product variant not found: {variantDesc}");
                    }
                }

                // Insert into customerpricerecord
                if (billDto.CustomerId.Value != 1)
                {
                    string priceRecordQuery = @"
                INSERT INTO customerpricerecord 
                (customer_id, bill_id, date, payment, remarks)
                VALUES 
                (@customer_id, @bill_id, @date, @payment, @remarks)";

                    using var priceCmd = new MySqlCommand(priceRecordQuery, con, (MySqlTransaction)tran);
                    priceCmd.Parameters.AddWithValue("@customer_id", billDto.CustomerId.Value);
                    priceCmd.Parameters.AddWithValue("@bill_id", billId);
                    priceCmd.Parameters.AddWithValue("@date", billDto.BillDate);
                    priceCmd.Parameters.AddWithValue("@payment", billDto.PaidAmount);
                    priceCmd.Parameters.AddWithValue("@remarks",
                        $"Bill: {billNumber} | Total: Rs. {billDto.TotalAmount:N2} | Paid: Rs. {billDto.PaidAmount:N2} | Due: Rs. {amountDue:N2}");
                    await priceCmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();
                _logger.LogInformation($"Bill {billNumber} created with ID {billId} by staff {staffId}, stock deducted for {billDto.Items.Count} items");

                // ✅ GET CUSTOMER NAME FOR PDF
                string customerName = "Walk-in Customer";
                if (billDto.CustomerId.Value != 1)
                {
                    string customerQuery = "SELECT full_name FROM customers WHERE customer_id = @customerId";
                    using var customerCmd = new MySqlCommand(customerQuery, con);
                    customerCmd.Parameters.AddWithValue("@customerId", billDto.CustomerId.Value);
                    var result = await customerCmd.ExecuteScalarAsync();
                    customerName = result?.ToString() ?? "Walk-in Customer";
                }

                // ✅ GENERATE PDF
                var billPdfData = new BillPdfData
                {
                    BillNumber = billNumber,
                    BillDate = billDto.BillDate,
                    CustomerName = customerName,
                    Items = pdfItems,
                    Subtotal = billDto.TotalAmount,
                    DiscountAmount = billDto.DiscountAmount,
                    TotalAmount = billDto.TotalAmount,
                    AmountPaid = billDto.PaidAmount,
                    AmountDue = amountDue
                };

                byte[] pdfBytes = _pdfService.GenerateBillPdf(billPdfData);

                // ✅ SAVE PDF TO DISK
                string pdfDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "bills");
                Directory.CreateDirectory(pdfDirectory);

                string pdfFileName = $"{billNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string pdfPath = Path.Combine(pdfDirectory, pdfFileName);

                await File.WriteAllBytesAsync(pdfPath, pdfBytes);

                _logger.LogInformation($"PDF generated: {pdfFileName}");

                // ✅ RETURN BILL WITH PDF INFO
                var bill = await GetBillByIdAsync(billId);

                return new BillWithPdfResponse
                {
                    Bill = bill!,
                    PdfFileName = pdfFileName,
                    PdfUrl = $"/bills/{pdfFileName}",
                    PdfBytes = pdfBytes
                };
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                _logger.LogError(ex, "Error creating bill");
                throw;
            }
        }   

        public async Task<List<Bill>> SearchBillsAsync(BillSearchDto searchDto)
        {
            var bills = new List<Bill>();
            var whereConditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (searchDto.StartDate.HasValue)
            {
                whereConditions.Add("b.bill_date >= @startDate");
                parameters.Add(new MySqlParameter("@startDate", searchDto.StartDate.Value));
            }

            if (searchDto.EndDate.HasValue)
            {
                whereConditions.Add("b.bill_date <= @endDate");
                parameters.Add(new MySqlParameter("@endDate", searchDto.EndDate.Value));
            }

            if (searchDto.CustomerId.HasValue)
            {
                whereConditions.Add("b.customer_id = @customerId");
                parameters.Add(new MySqlParameter("@customerId", searchDto.CustomerId.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchDto.BillNumber))
            {
                whereConditions.Add("b.bill_number LIKE @billNumber");
                parameters.Add(new MySqlParameter("@billNumber", $"%{searchDto.BillNumber}%"));
            }

            if (!string.IsNullOrWhiteSpace(searchDto.PaymentStatus))
            {
                whereConditions.Add("l.value = @paymentStatus");
                parameters.Add(new MySqlParameter("@paymentStatus", searchDto.PaymentStatus));
            }

            string whereClause = whereConditions.Count > 0
                ? "WHERE " + string.Join(" AND ", whereConditions)
                : "";

            string query = $@"
                SELECT 
                    b.*,
                    c.full_name AS customer_name,
                    l.value AS payment_status
                FROM bills b
                LEFT JOIN customers c ON b.customer_id = c.customer_id
                LEFT JOIN lookup l ON b.payment_status_id = l.lookup_id
                {whereClause}
                ORDER BY b.bill_date DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddRange(parameters.ToArray());
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    bills.Add(MapToBill(reader));
                }

                return bills;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching bills");
                throw;
            }
        }

        public async Task<List<Bill>> GetBillsByCustomerAsync(int customerId)
        {
            var bills = new List<Bill>();
            string query = @"
                SELECT 
                    b.*,
                    c.full_name AS customer_name,
                    l.value AS payment_status
                FROM bills b
                LEFT JOIN customers c ON b.customer_id = c.customer_id
                LEFT JOIN lookup l ON b.payment_status_id = l.lookup_id
                WHERE b.customer_id = @customerId
                ORDER BY b.bill_date DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@customerId", customerId);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    bills.Add(MapToBill(reader));
                }

                return bills;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving bills for customer {customerId}");
                throw;
            }
        }

        public async Task<List<Bill>> GetPendingBillsAsync()
        {
            var bills = new List<Bill>();
            string query = @"
                SELECT 
                    b.*,
                    c.full_name AS customer_name,
                    l.value AS payment_status
                FROM bills b
                LEFT JOIN customers c ON b.customer_id = c.customer_id
                LEFT JOIN lookup l ON b.payment_status_id = l.lookup_id
                WHERE l.value IN ('Pending', 'Partial')
                ORDER BY b.bill_date DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    bills.Add(MapToBill(reader));
                }

                return bills;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending bills");
                throw;
            }
        }

        public async Task<decimal> GetCustomerOutstandingBalanceAsync(int customerId)
        {
            string query = @"
                SELECT COALESCE(SUM(amount_due), 0) 
                FROM bills 
                WHERE customer_id = @customerId AND amount_due > 0";

            try
            {
                var parameters = new[] { new MySqlParameter("@customerId", customerId) };
                var result = await _db.ExecuteScalarAsync(query, parameters);
                return result != null ? Convert.ToDecimal(result) : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving outstanding balance for customer {customerId}");
                throw;
            }
        }

        private async Task<List<BillItem>> GetBillItemsAsync(int billId, MySqlConnection connection)
        {
            var items = new List<BillItem>();
            string query = @"
                SELECT 
                    bi.*,
                    p.name AS product_name,
                    pv.size
                FROM bill_items bi
                INNER JOIN products p ON bi.product_id = p.product_id
                INNER JOIN product_variants pv ON bi.variant_id = pv.variant_id
                WHERE bi.bill_id = @billId";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@billId", billId);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new BillItem
                {
                    BillItemId = reader.GetInt32("bill_item_id"),
                    BillId = reader.GetInt32("bill_id"),
                    ProductId = reader.GetInt32("product_id"),
                    VariantId = reader.GetInt32("variant_id"),
                    Quantity = reader.GetDecimal("quantity"),
                    UnitOfMeasure = reader.GetString("unit_of_measure"),
                    UnitPrice = reader.GetDecimal("unit_price"),
                    LineTotal = reader.GetDecimal("line_total"),
                    Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                    ProductName = reader.GetString("product_name"),
                    Size = reader.GetString("size")
                });
            }

            return items;
        }

        private Bill MapToBill(DbDataReader reader)
        {
            return new Bill
            {
                BillId = reader.GetInt32(reader.GetOrdinal("bill_id")),
                BillNumber = reader.GetString(reader.GetOrdinal("bill_number")),
                CustomerId = reader.IsDBNull(reader.GetOrdinal("customer_id"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("customer_id")),
                StaffId = reader.GetInt32(reader.GetOrdinal("staff_id")),
                BillDate = reader.GetDateTime(reader.GetOrdinal("bill_date")),
                Subtotal = reader.GetDecimal(reader.GetOrdinal("subtotal")),
                DiscountPercentage = reader.GetDecimal(reader.GetOrdinal("discount_percentage")),
                DiscountAmount = reader.GetDecimal(reader.GetOrdinal("discount_amount")),
                TaxPercentage = reader.GetDecimal(reader.GetOrdinal("tax_percentage")),
                TaxAmount = reader.GetDecimal(reader.GetOrdinal("tax_amount")),
                TotalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount")),
                AmountPaid = reader.GetDecimal(reader.GetOrdinal("amount_paid")),
                AmountDue = reader.GetDecimal(reader.GetOrdinal("amount_due")),
                PaymentStatusId = reader.GetInt32(reader.GetOrdinal("payment_status_id")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_name"))
                    ? "Walk-in Customer"
                    : reader.GetString(reader.GetOrdinal("customer_name")),
                PaymentStatus = reader.GetString(reader.GetOrdinal("payment_status"))
            };
        }
    }
}