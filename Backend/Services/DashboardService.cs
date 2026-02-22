using System.Data;
using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using MySql.Data.MySqlClient;

namespace HardwareStoreAPI.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(ILogger<DashboardService> logger)
        {
            _db = DatabaseHelper.Instance;
            _logger = logger;
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var stats = new DashboardStats();

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();

                // Today's Revenue
                stats.TodayRevenue = await ExecuteScalarDecimalAsync(connection, @"
                    SELECT COALESCE(SUM(total_amount), 0) 
                    FROM bills 
                    WHERE DATE(bill_date) = CURDATE()");

                stats.TodayBills = await ExecuteScalarIntAsync(connection, @"
                    SELECT COUNT(*) 
                    FROM bills 
                    WHERE DATE(bill_date) = CURDATE()");

                // Week Revenue
                stats.WeekRevenue = await ExecuteScalarDecimalAsync(connection, @"
                    SELECT COALESCE(SUM(total_amount), 0) 
                    FROM bills 
                    WHERE YEARWEEK(bill_date, 1) = YEARWEEK(CURDATE(), 1)");

                stats.WeekBills = await ExecuteScalarIntAsync(connection, @"
                    SELECT COUNT(*) 
                    FROM bills 
                    WHERE YEARWEEK(bill_date, 1) = YEARWEEK(CURDATE(), 1)");

                // Month Revenue
                stats.MonthRevenue = await ExecuteScalarDecimalAsync(connection, @"
                    SELECT COALESCE(SUM(total_amount), 0) 
                    FROM bills 
                    WHERE MONTH(bill_date) = MONTH(CURDATE()) 
                    AND YEAR(bill_date) = YEAR(CURDATE())");

                stats.MonthBills = await ExecuteScalarIntAsync(connection, @"
                    SELECT COUNT(*) 
                    FROM bills 
                    WHERE MONTH(bill_date) = MONTH(CURDATE()) 
                    AND YEAR(bill_date) = YEAR(CURDATE())");

                // Year Revenue
                stats.YearRevenue = await ExecuteScalarDecimalAsync(connection, @"
                    SELECT COALESCE(SUM(total_amount), 0) 
                    FROM bills 
                    WHERE YEAR(bill_date) = YEAR(CURDATE())");

                // Total Revenue
                stats.TotalRevenue = await ExecuteScalarDecimalAsync(connection, @"
                    SELECT COALESCE(SUM(total_amount), 0) FROM bills");

                stats.TotalBills = await ExecuteScalarIntAsync(connection, @"
                    SELECT COUNT(*) FROM bills");

                // Pending Bills
                stats.PendingBills = await ExecuteScalarIntAsync(connection, @"
                    SELECT COUNT(*) 
                    FROM bills b
                    JOIN lookup l ON b.payment_status_id = l.lookup_id
                    WHERE l.value IN ('Pending', 'Partial')");

                // Customers
                using (var cmd = new MySqlCommand(@"
                    SELECT 
                        COUNT(*) as total,
                        SUM(CASE WHEN is_active = 1 THEN 1 ELSE 0 END) as active,
                        COALESCE(SUM(current_balance), 0) as outstanding
                    FROM customers
                    WHERE customer_type != 'walkin'", connection))
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        stats.TotalCustomers = reader.GetInt32("total");
                        stats.ActiveCustomers = reader.GetInt32("active");
                        stats.TotalOutstanding = reader.GetDecimal("outstanding");
                    }
                }

                // Products
                stats.TotalProducts = await ExecuteScalarIntAsync(connection, @"
                    SELECT COUNT(DISTINCT p.product_id) 
                    FROM products p 
                    WHERE p.is_active = 1");

                // Stock
                using (var cmd = new MySqlCommand(@"
                    SELECT 
                        SUM(CASE WHEN quantity_in_stock <= reorder_level 
                                 AND quantity_in_stock > 0 THEN 1 ELSE 0 END) as low_stock,
                        SUM(CASE WHEN quantity_in_stock = 0 THEN 1 ELSE 0 END) as out_of_stock,
                        COALESCE(SUM(quantity_in_stock * price_per_unit), 0) as stock_value
                    FROM product_variants 
                    WHERE is_active = 1", connection))
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        stats.LowStockProducts = reader.GetInt32("low_stock");
                        stats.OutOfStockProducts = reader.GetInt32("out_of_stock");
                        stats.TotalStockValue = reader.GetDecimal("stock_value");
                    }
                }

                // Suppliers
                using (var cmd = new MySqlCommand(@"
                    SELECT 
                        COUNT(DISTINCT s.supplier_id) as total,
                        COALESCE(SUM(pb.total_price - pb.paid), 0) as pending,
                        COALESCE(SUM(pb.paid), 0) as paid
                    FROM supplier s
                    LEFT JOIN purchase_batches pb ON s.supplier_id = pb.supplier_id
                    WHERE s.is_active = 1", connection))
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        stats.TotalSuppliers = reader.GetInt32("total");
                        stats.SuppliersPending = reader.GetDecimal("pending");
                        stats.SuppliersPaid = reader.GetDecimal("paid");
                    }
                }

                // Quotations
                using (var cmd = new MySqlCommand(@"
                    SELECT 
                        COUNT(*) as total,
                        SUM(CASE WHEN l.value IN ('Draft', 'Sent') THEN 1 ELSE 0 END) as pending,
                        COALESCE(SUM(q.total_amount), 0) as value
                    FROM quotations q
                    JOIN lookup l ON q.status_id = l.lookup_id", connection))
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        stats.TotalQuotations = reader.GetInt32("total");
                        stats.PendingQuotations = reader.GetInt32("pending");
                        stats.QuotationsValue = reader.GetDecimal("value");
                    }
                }

                // Estimated Profit
                stats.EstimatedProfit = await ExecuteScalarDecimalAsync(connection, @"
                    SELECT 
                        (SELECT COALESCE(SUM(total_amount), 0) FROM bills) -
                        (SELECT COALESCE(SUM(total_price), 0) FROM purchase_batches) as profit");

                if (stats.TotalRevenue > 0)
                    stats.ProfitMargin = (stats.EstimatedProfit / stats.TotalRevenue) * 100;

                _logger.LogInformation("Dashboard stats retrieved successfully");
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard stats");
                throw;
            }
        }

        public async Task<List<TopProduct>> GetTopProductsAsync(int limit = 10)
        {
            var products = new List<TopProduct>();
            string query = @"
                SELECT 
                    p.name as ProductName,
                    pv.size as Size,
                    COUNT(bi.bill_item_id) as TotalSales,
                    SUM(bi.quantity) as QuantitySold,
                    SUM(bi.line_total) as Revenue
                FROM bill_items bi
                JOIN products p ON bi.product_id = p.product_id
                JOIN product_variants pv ON bi.variant_id = pv.variant_id
                GROUP BY p.product_id, pv.variant_id, p.name, pv.size
                ORDER BY Revenue DESC
                LIMIT @limit";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@limit", limit);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    products.Add(new TopProduct
                    {
                        ProductName = reader.GetString("ProductName"),
                        Size = reader.IsDBNull(reader.GetOrdinal("Size")) ? "Standard" : reader.GetString("Size"),
                        TotalSales = Convert.ToInt32(reader["TotalSales"]),
                        QuantitySold = Convert.ToInt32(reader["QuantitySold"]),
                        Revenue = reader.GetDecimal("Revenue")
                    });
                }

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top products");
                throw;
            }
        }

        public async Task<List<RecentBill>> GetRecentBillsAsync(int limit = 10)
        {
            var bills = new List<RecentBill>();
            string query = @"
                SELECT 
                    b.bill_number as BillNumber,
                    COALESCE(c.full_name, 'Walk-in') as CustomerName,
                    b.bill_date as BillDate,
                    b.total_amount as TotalAmount,
                    l.value as PaymentStatus,
                    b.amount_due as AmountDue
                FROM bills b
                LEFT JOIN customers c ON b.customer_id = c.customer_id
                JOIN lookup l ON b.payment_status_id = l.lookup_id
                ORDER BY b.bill_date DESC
                LIMIT @limit";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@limit", limit);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    bills.Add(new RecentBill
                    {
                        BillNumber = reader.GetString("BillNumber"),
                        CustomerName = reader.GetString("CustomerName"),
                        BillDate = reader.GetDateTime("BillDate"),
                        TotalAmount = reader.GetDecimal("TotalAmount"),
                        PaymentStatus = reader.GetString("PaymentStatus"),
                        AmountDue = reader.GetDecimal("AmountDue")
                    });
                }

                return bills;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent bills");
                throw;
            }
        }

        public async Task<List<LowStockItem>> GetLowStockItemsAsync(int limit = 10)
        {
            var items = new List<LowStockItem>();
            string query = @"
                SELECT 
                    p.name as ProductName,
                    pv.size as Size,
                    pv.quantity_in_stock as CurrentStock,
                    pv.reorder_level as ReorderLevel,
                    COALESCE(s.name, 'No Supplier') as SupplierName
                FROM product_variants pv
                JOIN products p ON pv.product_id = p.product_id
                LEFT JOIN supplier s ON p.supplier_id = s.supplier_id
                WHERE pv.quantity_in_stock <= pv.reorder_level
                AND pv.is_active = 1 AND p.is_active = 1
                ORDER BY (pv.reorder_level - pv.quantity_in_stock) DESC
                LIMIT @limit";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@limit", limit);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    items.Add(new LowStockItem
                    {
                        ProductName = reader.GetString("ProductName"),
                        Size = reader.IsDBNull(reader.GetOrdinal("Size")) ? "Standard" : reader.GetString("Size"),
                        CurrentStock = reader.GetDecimal("CurrentStock"),
                        ReorderLevel = reader.GetDecimal("ReorderLevel"),
                        SupplierName = reader.GetString("SupplierName")
                    });
                }

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving low stock items");
                throw;
            }
        }

        public async Task<List<SalesChartData>> GetSalesChartDataAsync(int days = 30)
        {
            var data = new List<SalesChartData>();
            string query = @"
                SELECT 
                    DATE(bill_date) as Period,
                    COALESCE(SUM(total_amount), 0) as Sales,
                    COUNT(*) as BillCount
                FROM bills
                WHERE bill_date >= DATE_SUB(CURDATE(), INTERVAL @days DAY)
                GROUP BY DATE(bill_date)
                ORDER BY Period";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@days", days);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    data.Add(new SalesChartData
                    {
                        Period = reader.GetDateTime("Period").ToString("MMM dd"),
                        Sales = reader.GetDecimal("Sales"),
                        BillCount = reader.GetInt32("BillCount")
                    });
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sales chart data");
                throw;
            }
        }

        public async Task<List<CategorySales>> GetCategorySalesAsync()
        {
            var categories = new List<CategorySales>();
            string query = @"
                SELECT 
                    l.value as CategoryName,
                    COALESCE(SUM(bi.line_total), 0) as TotalSales,
                    COUNT(DISTINCT bi.bill_item_id) as ItemCount
                FROM lookup l
                JOIN products p ON l.lookup_id = p.category_id
                JOIN bill_items bi ON p.product_id = bi.product_id
                WHERE l.type = 'category'
                GROUP BY l.lookup_id, l.value
                ORDER BY TotalSales DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                decimal totalSales = 0;
                while (await reader.ReadAsync())
                {
                    var sales = reader.GetDecimal("TotalSales");
                    totalSales += sales;

                    categories.Add(new CategorySales
                    {
                        CategoryName = reader.GetString("CategoryName"),
                        TotalSales = sales,
                        ItemCount = reader.GetInt32("ItemCount"),
                        Percentage = 0
                    });
                }

                // Calculate percentages
                if (totalSales > 0)
                {
                    foreach (var cat in categories)
                        cat.Percentage = (double)(cat.TotalSales / totalSales * 100);
                }

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category sales");
                throw;
            }
        }

        public async Task<List<PaymentMethodStats>> GetPaymentMethodStatsAsync()
        {
            var methods = new List<PaymentMethodStats>();
            string query = @"
                SELECT 
                    l.value as PaymentMethod,
                    COUNT(*) as Count,
                    COALESCE(SUM(b.total_amount), 0) as Amount
                FROM bills b
                JOIN lookup l ON b.payment_status_id = l.lookup_id
                GROUP BY l.lookup_id, l.value
                ORDER BY Amount DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                decimal totalAmount = 0;
                while (await reader.ReadAsync())
                {
                    var amount = reader.GetDecimal("Amount");
                    totalAmount += amount;

                    methods.Add(new PaymentMethodStats
                    {
                        PaymentMethod = reader.GetString("PaymentMethod"),
                        Count = reader.GetInt32("Count"),
                        Amount = amount,
                        Percentage = 0
                    });
                }

                // Calculate percentages
                if (totalAmount > 0)
                {
                    foreach (var method in methods)
                        method.Percentage = (double)(method.Amount / totalAmount * 100);
                }

                return methods;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment method stats");
                throw;
            }
        }

        // Helper methods
        private async Task<decimal> ExecuteScalarDecimalAsync(MySqlConnection connection, string query)
        {
            using var command = new MySqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
        }

        private async Task<int> ExecuteScalarIntAsync(MySqlConnection connection, string query)
        {
            using var command = new MySqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }
    }
}