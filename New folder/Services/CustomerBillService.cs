using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;

namespace HardwareStoreAPI.Services
{
    public class CustomerBillService : ICustomerBillService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<CustomerBillService> _logger;

        public CustomerBillService(ILogger<CustomerBillService> logger)
        {
            _db = DatabaseHelper.Instance;
            _logger = logger;
        }

        public async Task<List<CustomerBillSummary>> GetAllCustomerBillSummariesAsync(string? search = null)
        {
            var summaries = new List<CustomerBillSummary>();

            // Build WHERE clause based on search parameter
            string whereClause = string.IsNullOrWhiteSpace(search)
                ? ""
                : "WHERE c.full_name LIKE @search OR b.customer_id LIKE @search";

            string query = $@"
        SELECT 
            b.customer_id, 
            c.full_name, 
            COUNT(b.bill_id) AS bill_count,
            SUM(b.total_amount) AS total_amount, 
            SUM(b.amount_paid) AS paid, 
            (SUM(b.total_amount) - SUM(b.amount_paid)) AS remaining
        FROM bills b
        JOIN customers c ON b.customer_id = c.customer_id
        {whereClause}
        GROUP BY b.customer_id, c.full_name
        ORDER BY c.full_name";

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
                    summaries.Add(new CustomerBillSummary
                    {
                        CustomerId = reader.GetInt32("customer_id"),
                        CustomerName = reader.GetString("full_name"),
                        BillCount = reader.GetInt32("bill_count"),  // ✅ Now getting actual count
                        TotalAmount = reader.GetDecimal("total_amount"),
                        Paid = reader.GetDecimal("paid"),
                        Remaining = reader.GetDecimal("remaining")
                    });
                }

                _logger.LogInformation($"Retrieved {summaries.Count} customer bill summaries");
                return summaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer bill summaries");
                throw;
            }
        }

        public async Task<CustomerBillSummary?> GetCustomerBillSummaryAsync(int customerId)
        {
            string query = @"
                SELECT 
                    b.customer_id, 
                    c.full_name, 
                    SUM(b.total_amount) AS total_amount, 
                    SUM(b.amount_paid) AS paid, 
                    (SUM(b.total_amount) - SUM(b.amount_paid)) AS remaining,
                    COUNT(b.bill_id) AS bill_count
                FROM bills b
                JOIN customers c ON b.customer_id = c.customer_id
                WHERE b.customer_id = @customerId
                GROUP BY b.customer_id, c.full_name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@customerId", customerId);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new CustomerBillSummary
                    {
                        CustomerId = reader.GetInt32("customer_id"),
                        CustomerName = reader.GetString("full_name"),
                        TotalAmount = reader.GetDecimal("total_amount"),
                        Paid = reader.GetDecimal("paid"),
                        Remaining = reader.GetDecimal("remaining"),
                        BillCount = reader.GetInt32("bill_count")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving bill summary for customer {customerId}");
                throw;
            }
        }

        public async Task<List<CustomerBillDetail>> GetCustomerBillsAsync(int customerId)
        {
            var bills = new List<CustomerBillDetail>();
            string query = @"
                SELECT 
                    b.bill_id,
                    b.bill_number,
                    b.customer_id,
                    c.full_name AS customer_name,
                    b.bill_date,
                    b.total_amount,
                    b.amount_paid,
                    b.amount_due,
                    l.value AS payment_status
                FROM bills b
                JOIN customers c ON b.customer_id = c.customer_id
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
                    bills.Add(new CustomerBillDetail
                    {
                        BillId = reader.GetInt32("bill_id"),
                        BillNumber = reader.GetString("bill_number"),
                        CustomerId = reader.GetInt32("customer_id"),
                        CustomerName = reader.GetString("customer_name"),
                        BillDate = reader.GetDateTime("bill_date"),
                        TotalAmount = reader.GetDecimal("total_amount"),
                        AmountPaid = reader.GetDecimal("amount_paid"),
                        AmountDue = reader.GetDecimal("amount_due"),
                        PaymentStatus = reader.GetString("payment_status")
                    });
                }

                return bills;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving bills for customer {customerId}");
                throw;
            }
        }

        public async Task<CustomerBillDetail?> GetBillDetailAsync(int billId)
        {
            string billQuery = @"
        SELECT 
            b.bill_id,
            b.bill_number,
            b.customer_id,
            c.full_name AS customer_name,
            b.bill_date,
            b.subtotal,   
            b.discount_amount,         
            b.total_amount,
            b.amount_paid,
            b.amount_due,
            l.value AS payment_status
        FROM bills b
        JOIN customers c ON b.customer_id = c.customer_id
        LEFT JOIN lookup l ON b.payment_status_id = l.lookup_id
        WHERE b.bill_id = @billId";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(billQuery, connection);
                command.Parameters.AddWithValue("@billId", billId);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var bill = new CustomerBillDetail
                    {
                        BillId = reader.GetInt32("bill_id"),
                        BillNumber = reader.GetString("bill_number"),
                        CustomerId = reader.GetInt32("customer_id"),
                        CustomerName = reader.GetString("customer_name"),
                        BillDate = reader.GetDateTime("bill_date"),
                        Subtotal = reader.GetDecimal("subtotal"),              // ✅ ADD
                        DiscountAmount = reader.GetDecimal("discount_amount"), // ✅ ADD
                        TotalAmount = reader.GetDecimal("total_amount"),
                        AmountPaid = reader.GetDecimal("amount_paid"),
                        AmountDue = reader.GetDecimal("amount_due"),
                        PaymentStatus = reader.GetString("payment_status")
                    };

                    await reader.CloseAsync();

                    // Get bill items
                    bill.Items = await GetBillItemsAsync(billId, connection);

                    return bill;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving bill detail for bill {billId}");
                throw;
            }
        }

        public async Task<List<CustomerPaymentRecord>> GetCustomerPaymentRecordsAsync(int customerId)
        {
            var records = new List<CustomerPaymentRecord>();
            string query = @"
                SELECT 
                    pr.record_id,
                    pr.customer_id,
                    pr.bill_id,
                    pr.date,
                    pr.payment,
                    pr.remarks,
                    b.bill_number
                FROM customerpricerecord pr
                LEFT JOIN bills b ON pr.bill_id = b.bill_id
                WHERE pr.customer_id = @customerId
                ORDER BY pr.date DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@customerId", customerId);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    records.Add(new CustomerPaymentRecord
                    {
                        RecordId = reader.GetInt32("record_id"),
                        CustomerId = reader.GetInt32("customer_id"),
                        BillId = reader.IsDBNull(reader.GetOrdinal("bill_id"))
                            ? null
                            : reader.GetInt32("bill_id"),
                        Date = reader.GetDateTime("date"),
                        Payment = reader.GetDecimal("payment"),
                        Remarks = reader.IsDBNull(reader.GetOrdinal("remarks"))
                            ? null
                            : reader.GetString("remarks"),
                        BillNumber = reader.IsDBNull(reader.GetOrdinal("bill_number"))
                            ? null
                            : reader.GetString("bill_number")
                    });
                }

                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving payment records for customer {customerId}");
                throw;
            }
        }

        public async Task<PaymentDistributionResult> AddCustomerPaymentAsync(AddCustomerPaymentDto paymentDto)
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

                // 1. Fetch unpaid bills (FIFO - First In First Out)
                string selectQuery = @"
                    SELECT bill_id, bill_number, total_amount, amount_paid, amount_due
                    FROM bills
                    WHERE customer_id = @customerId 
                    AND amount_due > 0
                    ORDER BY bill_date ASC, bill_id ASC";

                var bills = new List<(int id, string number, decimal total, decimal paid, decimal due)>();
                using (var cmd = new MySqlCommand(selectQuery, con, (MySqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@customerId", paymentDto.CustomerId);
                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        bills.Add((
                            reader.GetInt32("bill_id"),
                            reader.GetString("bill_number"),
                            reader.GetDecimal("total_amount"),
                            reader.GetDecimal("amount_paid"),
                            reader.GetDecimal("amount_due")
                        ));
                    }
                }

                decimal remainingPayment = paymentDto.PaymentAmount;

                // 2. Distribute payment across bills
                foreach (var bill in bills)
                {
                    if (remainingPayment <= 0) break;

                    decimal toPay = Math.Min(remainingPayment, bill.due);

                    if (toPay > 0)
                    {
                        // Update bill paid amount
                        string updateBillQuery = @"
                            UPDATE bills 
                            SET amount_paid = amount_paid + @toPay,
                                amount_due = amount_due - @toPay,
                                updated_at = NOW()
                            WHERE bill_id = @bill_id";

                        using (var updateCmd = new MySqlCommand(updateBillQuery, con, (MySqlTransaction)tran))
                        {
                            updateCmd.Parameters.AddWithValue("@toPay", toPay);
                            updateCmd.Parameters.AddWithValue("@bill_id", bill.id);
                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        // Update payment status if bill is fully paid
                        if (toPay == bill.due)
                        {
                            string updateStatusQuery = @"
                                UPDATE bills 
                                SET payment_status_id = (SELECT lookup_id FROM lookup WHERE type = 'payment_status' AND value = 'Paid')
                                WHERE bill_id = @bill_id";

                            using var statusCmd = new MySqlCommand(updateStatusQuery, con, (MySqlTransaction)tran);
                            statusCmd.Parameters.AddWithValue("@bill_id", bill.id);
                            await statusCmd.ExecuteNonQueryAsync();
                        }

                        // Record payment in customerpricerecord
                        string insertPaymentQuery = @"
                            INSERT INTO customerpricerecord 
                            (customer_id, bill_id, date, payment, remarks)
                            VALUES (@customerId, @billId, @date, @amount, @remarks)";

                        using (var cmdInsert = new MySqlCommand(insertPaymentQuery, con, (MySqlTransaction)tran))
                        {
                            cmdInsert.Parameters.AddWithValue("@customerId", paymentDto.CustomerId);
                            cmdInsert.Parameters.AddWithValue("@billId", bill.id);
                            cmdInsert.Parameters.AddWithValue("@date", DateTime.Now);
                            cmdInsert.Parameters.AddWithValue("@amount", toPay);
                            cmdInsert.Parameters.AddWithValue("@remarks",
                                paymentDto.Remarks ?? $"Payment applied to bill #{bill.number}");
                            await cmdInsert.ExecuteNonQueryAsync();
                        }

                        // Update customer balance
                        string updateBalanceQuery = @"
                            UPDATE customers 
                            SET current_balance = current_balance - @amount,
                                updated_at = NOW()
                            WHERE customer_id = @customerId";

                        using (var balanceCmd = new MySqlCommand(updateBalanceQuery, con, (MySqlTransaction)tran))
                        {
                            balanceCmd.Parameters.AddWithValue("@amount", toPay);
                            balanceCmd.Parameters.AddWithValue("@customerId", paymentDto.CustomerId);
                            await balanceCmd.ExecuteNonQueryAsync();
                        }

                        // Track allocation
                        result.Allocations.Add(new BillPaymentAllocation
                        {
                            BillId = bill.id,
                            BillNumber = bill.number,
                            BillDueBefore = bill.due,
                            PaymentApplied = toPay,
                            BillDueAfter = bill.due - toPay
                        });

                        result.Applied += toPay;
                        remainingPayment -= toPay;
                    }
                }

                // 3. Handle overpayment as credit
                if (remainingPayment > 0)
                {
                    string overpaymentQuery = @"
                        INSERT INTO customerpricerecord 
                        (customer_id, bill_id, date, payment, remarks) 
                        VALUES (@customerId, @billId, @date, @amount, @remarks)";

                    using (var overCmd = new MySqlCommand(overpaymentQuery, con, (MySqlTransaction)tran))
                    {
                        overCmd.Parameters.AddWithValue("@customerId", paymentDto.CustomerId);
                        overCmd.Parameters.AddWithValue("@billId", DBNull.Value);
                        overCmd.Parameters.AddWithValue("@date", DateTime.Now);
                        overCmd.Parameters.AddWithValue("@amount", remainingPayment);
                        overCmd.Parameters.AddWithValue("@remarks",
                            $"Credit balance (overpayment) - {paymentDto.Remarks ?? ""}");
                        await overCmd.ExecuteNonQueryAsync();
                    }

                    // Still update customer balance
                    string updateBalanceQuery = @"
                        UPDATE customers 
                        SET current_balance = current_balance - @amount,
                            updated_at = NOW()
                        WHERE customer_id = @customerId";

                    using var balanceCmd = new MySqlCommand(updateBalanceQuery, con, (MySqlTransaction)tran);
                    balanceCmd.Parameters.AddWithValue("@amount", remainingPayment);
                    balanceCmd.Parameters.AddWithValue("@customerId", paymentDto.CustomerId);
                    await balanceCmd.ExecuteNonQueryAsync();
                }

                result.Remaining = remainingPayment;

                await tran.CommitAsync();
                _logger.LogInformation($"Payment of Rs. {paymentDto.PaymentAmount} added for customer {paymentDto.CustomerId}");

                return result;
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                _logger.LogError(ex, "Error adding customer payment");
                throw;
            }
        }

        private async Task<List<BillItemDetail>> GetBillItemsAsync(int billId, MySqlConnection connection)
        {
            var items = new List<BillItemDetail>();
            string query = @"
                SELECT 
                    bi.bill_item_id,
                    p.name AS product_name,
                    pv.size,
                    bi.unit_of_measure,
                    bi.quantity,
                    bi.unit_price,
                    bi.line_total
                FROM bill_items bi
                JOIN products p ON bi.product_id = p.product_id
                JOIN product_variants pv ON bi.variant_id = pv.variant_id
                WHERE bi.bill_id = @billId
                ORDER BY bi.bill_item_id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@billId", billId);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new BillItemDetail
                {
                    BillItemId = reader.GetInt32("bill_item_id"),
                    ProductName = reader.GetString("product_name"),
                    Size = reader.GetString("size"),
                    UnitOfMeasure = reader.GetString("unit_of_measure"),
                    Quantity = reader.GetDecimal("quantity"),
                    UnitPrice = reader.GetDecimal("unit_price"),
                    LineTotal = reader.GetDecimal("line_total")
                });
            }

            return items;
        }
    }
}