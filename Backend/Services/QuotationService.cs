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
    public class QuotationService : IQuotationService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<QuotationService> _logger;
        private readonly IPdfService _pdfService;  
        private readonly IHttpContextAccessor _httpContextAccessor;

        public QuotationService(ILogger<QuotationService> logger, IPdfService pdfService, IHttpContextAccessor httpContextAccessor)
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
                return 1;
            }

            var staffIdClaim = user.FindFirst("StaffId")?.Value
                ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            if (int.TryParse(staffIdClaim, out int staffId))
            {
                _logger.LogInformation($"Extracted staff ID from token: {staffId}");
                return staffId;
            }

            _logger.LogWarning("Could not parse staff ID from token, defaulting to staff_id = 1");
            return 1;
        }

        public async Task<List<Quotation>> GetAllQuotationsAsync()
        {
            var quotations = new List<Quotation>();
            string query = @"
                SELECT 
                    q.*,
                    c.full_name AS customer_name,
                    c.phone AS customer_contact,
                    s.name AS staff_name,
                    l.value AS status
                FROM quotations q
                LEFT JOIN customers c ON q.customer_id = c.customer_id
                LEFT JOIN staff s ON q.staff_id = s.staff_id
                LEFT JOIN lookup l ON q.status_id = l.lookup_id
                ORDER BY q.quotation_date DESC, q.quotation_id DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    quotations.Add(MapToQuotation(reader));
                }

                _logger.LogInformation($"Retrieved {quotations.Count} quotations");
                return quotations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all quotations");
                throw;
            }
        }

        public async Task<PaginatedResponse<Quotation>> GetQuotationsPaginatedAsync(int pageNumber, int pageSize, QuotationSearchDto? filters = null)
        {
            var response = new PaginatedResponse<Quotation>
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
                        whereConditions.Add("q.quotation_date >= @startDate");
                        parameters.Add(new MySqlParameter("@startDate", filters.StartDate.Value));
                    }

                    if (filters.EndDate.HasValue)
                    {
                        whereConditions.Add("q.quotation_date <= @endDate");
                        parameters.Add(new MySqlParameter("@endDate", filters.EndDate.Value));
                    }

                    if (filters.CustomerId.HasValue)
                    {
                        whereConditions.Add("q.customer_id = @customerId");
                        parameters.Add(new MySqlParameter("@customerId", filters.CustomerId.Value));
                    }

                    if (!string.IsNullOrWhiteSpace(filters.QuotationNumber))
                    {
                        whereConditions.Add("q.quotation_number LIKE @quotationNumber");
                        parameters.Add(new MySqlParameter("@quotationNumber", $"%{filters.QuotationNumber}%"));
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
                    FROM quotations q
                    LEFT JOIN lookup l ON q.status_id = l.lookup_id
                    {whereClause}";

                response.TotalRecords = Convert.ToInt32(
                    await _db.ExecuteScalarAsync(countQuery, parameters.ToArray()));

                // Get paginated data
                int offset = (pageNumber - 1) * pageSize;
                string query = $@"
                    SELECT 
                        q.*,
                        c.full_name AS customer_name,
                        c.phone AS customer_contact,
                        s.name AS staff_name,
                        l.value AS status
                    FROM quotations q
                    LEFT JOIN customers c ON q.customer_id = c.customer_id
                    LEFT JOIN staff s ON q.staff_id = s.staff_id
                    LEFT JOIN lookup l ON q.status_id = l.lookup_id
                    {whereClause}
                    ORDER BY q.quotation_date DESC, q.quotation_id DESC
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
                    response.Data.Add(MapToQuotation(reader));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated quotations");
                throw;
            }
        }

        public async Task<Quotation?> GetQuotationByIdAsync(int id)
        {
            string query = @"
                SELECT 
                    q.*,
                    c.full_name AS customer_name,
                    c.phone AS customer_contact,
                    s.name AS staff_name,
                    l.value AS status
                FROM quotations q
                LEFT JOIN customers c ON q.customer_id = c.customer_id
                LEFT JOIN staff s ON q.staff_id = s.staff_id
                LEFT JOIN lookup l ON q.status_id = l.lookup_id
                WHERE q.quotation_id = @id";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var quotation = MapToQuotation(reader);
                    var quotationId = quotation.QuotationId;
                    await reader.CloseAsync();

                    // Load quotation items
                    quotation.Items = await GetQuotationItemsAsync(quotationId, connection);
                    return quotation;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving quotation with ID {id}");
                throw;
            }
        }

        public async Task<Quotation?> GetQuotationByNumberAsync(string quotationNumber)
        {
            string query = @"
                SELECT 
                    q.*,
                    c.full_name AS customer_name,
                    c.phone AS customer_contact,
                    s.name AS staff_name,
                    l.value AS status
                FROM quotations q
                LEFT JOIN customers c ON q.customer_id = c.customer_id
                LEFT JOIN staff s ON q.staff_id = s.staff_id
                LEFT JOIN lookup l ON q.status_id = l.lookup_id
                WHERE q.quotation_number = @quotationNumber";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@quotationNumber", quotationNumber);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var quotation = MapToQuotation(reader);
                    var quotationId = quotation.QuotationId;
                    await reader.CloseAsync();

                    quotation.Items = await GetQuotationItemsAsync(quotationId, connection);
                    return quotation;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving quotation with number {quotationNumber}");
                throw;
            }
        }

        public async Task<Quotation?> SearchQuotationAsync(string searchValue)
        {
            try
            {
                // Try to parse as ID first
                if (int.TryParse(searchValue, out int id))
                {
                    var quotation = await GetQuotationByIdAsync(id);
                    if (quotation != null)
                        return quotation;
                }

                // Otherwise search by quotation number
                return await GetQuotationByNumberAsync(searchValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching quotation with value {searchValue}");
                throw;
            }
        }

        public async Task<QuotationWithPdfResponse> CreateQuotationAsync(CreateQuotationDto quotationDto, int staffId = 1)
        {
            int actualStaffId = GetCurrentStaffId();
            _logger.LogInformation($"Creating quotation with staff ID: {actualStaffId}");

            using var con = _db.GetConnection();
            await con.OpenAsync();
            using var tran = await con.BeginTransactionAsync();

            try
            {
                if (quotationDto.Items == null || quotationDto.Items.Count == 0)
                    throw new Exception("No products selected for quotation");

                string quotationNumber = $"QUO-{DateTime.Now:yyyy}-{DateTime.Now:MMddHHmmss}";
                _logger.LogInformation($"Generated quotation number: {quotationNumber}");

                // Get quotation status
                int statusId;
                string statusQuery = "SELECT lookup_id FROM lookup WHERE type = 'quotation_status' AND value = 'Draft' LIMIT 1";
                using (var statusCmd = new MySqlCommand(statusQuery, con, (MySqlTransaction)tran))
                {
                    var statusResult = await statusCmd.ExecuteScalarAsync();
                    if (statusResult == null)
                    {
                        statusQuery = "SELECT lookup_id FROM lookup WHERE type = 'quotation_status' LIMIT 1";
                        statusCmd.CommandText = statusQuery;
                        statusResult = await statusCmd.ExecuteScalarAsync();
                        if (statusResult == null)
                            throw new Exception("No quotation_status found in lookup table");
                    }
                    statusId = Convert.ToInt32(statusResult);
                }

                // ✅ GET CUSTOMER NAME BEFORE INSERTING (within transaction)
                string customerName = "Walk-in Customer";
                if (quotationDto.CustomerId.HasValue && quotationDto.CustomerId.Value > 0)
                {
                    string customerQuery = "SELECT full_name FROM customers WHERE customer_id = @customerId";
                    using var customerCmd = new MySqlCommand(customerQuery, con, (MySqlTransaction)tran);
                    customerCmd.Parameters.AddWithValue("@customerId", quotationDto.CustomerId.Value);
                    var result = await customerCmd.ExecuteScalarAsync();
                    customerName = result?.ToString() ?? "Walk-in Customer";
                }

                // Insert quotation
                string quotationQuery = @"
            INSERT INTO quotations 
            (quotation_number, quotation_date, customer_id, staff_id, subtotal, 
             discount_amount, total_amount, status_id, valid_until, notes, terms_conditions) 
            VALUES 
            (@quotation_number, @quotation_date, @customer_id, @staff_id, @subtotal, 
             @discount_amount, @total_amount, @status_id, @valid_until, @notes, @terms_conditions);
            SELECT LAST_INSERT_ID();";

                int quotationId;
                using (var cmd = new MySqlCommand(quotationQuery, con, (MySqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@quotation_number", quotationNumber);
                    cmd.Parameters.AddWithValue("@quotation_date", quotationDto.QuotationDate);
                    cmd.Parameters.AddWithValue("@customer_id", quotationDto.CustomerId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@staff_id", actualStaffId);
                    cmd.Parameters.AddWithValue("@subtotal", quotationDto.TotalAmount);
                    cmd.Parameters.AddWithValue("@discount_amount", quotationDto.DiscountAmount);
                    cmd.Parameters.AddWithValue("@total_amount", quotationDto.TotalAmount);
                    cmd.Parameters.AddWithValue("@status_id", statusId);
                    cmd.Parameters.AddWithValue("@valid_until", quotationDto.ValidUntil ?? DateTime.Now.AddDays(30));
                    cmd.Parameters.AddWithValue("@notes", quotationDto.Notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@terms_conditions", quotationDto.TermsConditions ?? (object)DBNull.Value);

                    var result = await cmd.ExecuteScalarAsync();
                    if (result == null)
                        throw new Exception("Failed to get quotation ID");
                    quotationId = Convert.ToInt32(result);
                }

                var pdfItems = new List<QuotationItemPdfData>();

                // Process each item
                foreach (var item in quotationDto.Items)
                {
                    if (string.IsNullOrEmpty(item.ProductName) || item.Quantity <= 0)
                        continue;

                    string productQuery = @"
                SELECT 
                    p.product_id, 
                    pv.variant_id,
                    pv.price_per_unit,
                    pv.unit_of_measure
                FROM products p
                INNER JOIN product_variants pv ON p.product_id = pv.product_id
                WHERE p.name = @ProductName 
                AND pv.size = @size
                AND p.is_active = TRUE 
                AND pv.is_active = TRUE";

                    using var productCmd = new MySqlCommand(productQuery, con, (MySqlTransaction)tran);
                    productCmd.Parameters.AddWithValue("@ProductName", item.ProductName);
                    productCmd.Parameters.AddWithValue("@size", string.IsNullOrEmpty(item.Size) ? "" : item.Size);

                    using var reader = await productCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        int productId = reader.GetInt32("product_id");
                        int variantId = reader.GetInt32("variant_id");
                        decimal salePrice = reader.GetDecimal("price_per_unit");
                        string unitOfMeasure = reader.GetString("unit_of_measure");
                        await reader.CloseAsync();

                        decimal unitPrice = item.UnitPrice ?? salePrice;
                        decimal lineTotal = unitPrice * item.Quantity;

                        pdfItems.Add(new QuotationItemPdfData
                        {
                            ProductName = item.ProductName,
                            Size = item.Size,
                            Quantity = item.Quantity,
                            UnitOfMeasure = unitOfMeasure,
                            UnitPrice = unitPrice,
                            LineTotal = lineTotal
                        });

                        string quotationItemQuery = @"
                    INSERT INTO quotation_items 
                    (quotation_id, product_id, variant_id, quantity, unit_of_measure, 
                     unit_price, line_total, notes) 
                    VALUES 
                    (@quotation_id, @product_id, @variant_id, @quantity, @unit_of_measure, 
                     @unit_price, @line_total, @notes)";

                        using var quotationItemCmd = new MySqlCommand(quotationItemQuery, con, (MySqlTransaction)tran);
                        quotationItemCmd.Parameters.AddWithValue("@quotation_id", quotationId);
                        quotationItemCmd.Parameters.AddWithValue("@product_id", productId);
                        quotationItemCmd.Parameters.AddWithValue("@variant_id", variantId);
                        quotationItemCmd.Parameters.AddWithValue("@quantity", item.Quantity);
                        quotationItemCmd.Parameters.AddWithValue("@unit_of_measure", unitOfMeasure);
                        quotationItemCmd.Parameters.AddWithValue("@unit_price", unitPrice);
                        quotationItemCmd.Parameters.AddWithValue("@line_total", lineTotal);
                        quotationItemCmd.Parameters.AddWithValue("@notes", item.Notes ?? (object)DBNull.Value);
                        await quotationItemCmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        await reader.CloseAsync();
                    }
                }

                if (pdfItems.Count == 0)
                    throw new Exception("No valid products were added to the quotation");

                // ✅ COMMIT TRANSACTION - All database operations done
                await tran.CommitAsync();
                _logger.LogInformation($"Quotation {quotationNumber} created with ID {quotationId} by staff {actualStaffId}");

                // ✅ GENERATE PDF (AFTER transaction is committed - no more DB operations with transaction)
                var quotationPdfData = new QuotationPdfData
                {
                    QuotationNumber = quotationNumber,
                    QuotationDate = quotationDto.QuotationDate,
                    CustomerName = customerName,  // ✅ Already retrieved above
                    Items = pdfItems,
                    Subtotal = quotationDto.TotalAmount,
                    DiscountAmount = quotationDto.DiscountAmount,
                    TotalAmount = quotationDto.TotalAmount,
                    ValidUntil = quotationDto.ValidUntil ?? DateTime.Now.AddDays(30)
                };

                byte[] pdfBytes = _pdfService.GenerateQuotationPdf(quotationPdfData);

                // ✅ SAVE PDF TO DISK
                string pdfDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "quotations");
                Directory.CreateDirectory(pdfDirectory);

                string pdfFileName = $"{quotationNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string pdfPath = Path.Combine(pdfDirectory, pdfFileName);

                await File.WriteAllBytesAsync(pdfPath, pdfBytes);

                _logger.LogInformation($"Quotation PDF generated: {pdfFileName}");

                // ✅ RETURN QUOTATION WITH PDF INFO
                var quotation = await GetQuotationByIdAsync(quotationId);

                return new QuotationWithPdfResponse
                {
                    Quotation = quotation!,
                    PdfFileName = pdfFileName,
                    PdfUrl = $"/quotations/{pdfFileName}",
                    PdfBytes = pdfBytes
                };
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                _logger.LogError(ex, "Error creating quotation");
                throw new Exception("Failed to save quotation: " + ex.Message, ex);
            }
        }
        public async Task<List<Quotation>> SearchQuotationsAsync(QuotationSearchDto searchDto)
        {
            var quotations = new List<Quotation>();
            var whereConditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (searchDto.StartDate.HasValue)
            {
                whereConditions.Add("q.quotation_date >= @startDate");
                parameters.Add(new MySqlParameter("@startDate", searchDto.StartDate.Value));
            }

            if (searchDto.EndDate.HasValue)
            {
                whereConditions.Add("q.quotation_date <= @endDate");
                parameters.Add(new MySqlParameter("@endDate", searchDto.EndDate.Value));
            }

            if (searchDto.CustomerId.HasValue)
            {
                whereConditions.Add("q.customer_id = @customerId");
                parameters.Add(new MySqlParameter("@customerId", searchDto.CustomerId.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchDto.QuotationNumber))
            {
                whereConditions.Add("q.quotation_number LIKE @quotationNumber");
                parameters.Add(new MySqlParameter("@quotationNumber", $"%{searchDto.QuotationNumber}%"));
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
                    q.*,
                    c.full_name AS customer_name,
                    c.phone AS customer_contact,
                    s.name AS staff_name,
                    l.value AS status
                FROM quotations q
                LEFT JOIN customers c ON q.customer_id = c.customer_id
                LEFT JOIN staff s ON q.staff_id = s.staff_id
                LEFT JOIN lookup l ON q.status_id = l.lookup_id
                {whereClause}
                ORDER BY q.quotation_date DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddRange(parameters.ToArray());
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    quotations.Add(MapToQuotation(reader));
                }

                return quotations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching quotations");
                throw;
            }
        }

        public async Task<List<Quotation>> GetQuotationsByCustomerAsync(int customerId)
        {
            var quotations = new List<Quotation>();
            string query = @"
                SELECT 
                    q.*,
                    c.full_name AS customer_name,
                    c.phone AS customer_contact,
                    s.name AS staff_name,
                    l.value AS status
                FROM quotations q
                LEFT JOIN customers c ON q.customer_id = c.customer_id
                LEFT JOIN staff s ON q.staff_id = s.staff_id
                LEFT JOIN lookup l ON q.status_id = l.lookup_id
                WHERE q.customer_id = @customerId
                ORDER BY q.quotation_date DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@customerId", customerId);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    quotations.Add(MapToQuotation(reader));
                }

                return quotations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving quotations for customer {customerId}");
                throw;
            }
        }

        public async Task<List<Quotation>> GetPendingQuotationsAsync()
        {
            var quotations = new List<Quotation>();
            string query = @"
                SELECT 
                    q.*,
                    c.full_name AS customer_name,
                    c.phone AS customer_contact,
                    s.name AS staff_name,
                    l.value AS status
                FROM quotations q
                LEFT JOIN customers c ON q.customer_id = c.customer_id
                LEFT JOIN staff s ON q.staff_id = s.staff_id
                LEFT JOIN lookup l ON q.status_id = l.lookup_id
                WHERE l.value IN ('Draft', 'Sent')
                ORDER BY q.quotation_date DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    quotations.Add(MapToQuotation(reader));
                }

                return quotations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending quotations");
                throw;
            }
        }

        public async Task<bool> ConvertQuotationToBillAsync(ConvertQuotationToBillDto convertDto, int staffId = 1)
        {
            // This would convert a quotation to a bill
            // Implementation would be similar to CreateBillAsync but using quotation data
            throw new NotImplementedException("Convert quotation to bill - to be implemented");
        }

        private async Task<List<QuotationItem>> GetQuotationItemsAsync(int quotationId, MySqlConnection connection)
        {
            var items = new List<QuotationItem>();
            string query = @"
                SELECT 
                    qi.*,
                    p.name AS product_name,
                    pv.size,
                    pv.class_type,
                    pv.quantity_in_stock AS available_stock,
                    sup.name AS supplier_name,
                    cat.value AS category
                FROM quotation_items qi
                INNER JOIN products p ON qi.product_id = p.product_id
                INNER JOIN product_variants pv ON qi.variant_id = pv.variant_id
                LEFT JOIN supplier sup ON p.supplier_id = sup.supplier_id
                LEFT JOIN lookup cat ON p.category_id = cat.lookup_id
                WHERE qi.quotation_id = @quotationId
                ORDER BY qi.quotation_item_id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@quotationId", quotationId);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new QuotationItem
                {
                    QuotationItemId = reader.GetInt32("quotation_item_id"),
                    QuotationId = reader.GetInt32("quotation_id"),
                    ProductId = reader.GetInt32("product_id"),
                    VariantId = reader.GetInt32("variant_id"),
                    Quantity = reader.GetDecimal("quantity"),
                    UnitOfMeasure = reader.GetString("unit_of_measure"),
                    UnitPrice = reader.GetDecimal("unit_price"),
                    LineTotal = reader.GetDecimal("line_total"),
                    Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                    ProductName = reader.GetString("product_name"),
                    Size = reader.GetString("size"),
                    ClassType = reader.IsDBNull(reader.GetOrdinal("class_type")) ? null : reader.GetString("class_type"),
                    AvailableStock = reader.GetDecimal("available_stock"),
                    SupplierName = reader.IsDBNull(reader.GetOrdinal("supplier_name")) ? null : reader.GetString("supplier_name"),
                    Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString("category")
                });
            }

            return items;
        }

        private Quotation MapToQuotation(DbDataReader reader)
        {
            return new Quotation
            {
                QuotationId = reader.GetInt32(reader.GetOrdinal("quotation_id")),
                QuotationNumber = reader.GetString(reader.GetOrdinal("quotation_number")),
                CustomerId = reader.IsDBNull(reader.GetOrdinal("customer_id"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("customer_id")),
                StaffId = reader.GetInt32(reader.GetOrdinal("staff_id")),
                QuotationDate = reader.GetDateTime(reader.GetOrdinal("quotation_date")),
                ValidUntil = reader.IsDBNull(reader.GetOrdinal("valid_until"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("valid_until")),
                Subtotal = reader.GetDecimal(reader.GetOrdinal("subtotal")),
                DiscountPercentage = reader.GetDecimal(reader.GetOrdinal("discount_percentage")),
                DiscountAmount = reader.GetDecimal(reader.GetOrdinal("discount_amount")),
                TaxPercentage = reader.GetDecimal(reader.GetOrdinal("tax_percentage")),
                TaxAmount = reader.GetDecimal(reader.GetOrdinal("tax_amount")),
                TotalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount")),
                StatusId = reader.GetInt32(reader.GetOrdinal("status_id")),
                ConvertedBillId = reader.IsDBNull(reader.GetOrdinal("converted_bill_id"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("converted_bill_id")),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                TermsConditions = reader.IsDBNull(reader.GetOrdinal("terms_conditions")) ? null : reader.GetString("terms_conditions"),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_name"))
                    ? "Walk-in Customer"
                    : reader.GetString(reader.GetOrdinal("customer_name")),
                CustomerContact = reader.IsDBNull(reader.GetOrdinal("customer_contact"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("customer_contact")),
                StaffName = reader.GetString(reader.GetOrdinal("staff_name")),
                Status = reader.GetString(reader.GetOrdinal("status"))
            };
        }
    }
}