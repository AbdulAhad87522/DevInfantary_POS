using System;

namespace HardwareStoreAPI.Models
{
    // Dashboard Statistics Model
    public class DashboardStats
    {
        // Revenue & Sales
        public decimal TodayRevenue { get; set; }
        public decimal WeekRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal YearRevenue { get; set; }
        public decimal TotalRevenue { get; set; }

        // Bills & Orders
        public int TodayBills { get; set; }
        public int WeekBills { get; set; }
        public int MonthBills { get; set; }
        public int TotalBills { get; set; }
        public int PendingBills { get; set; }

        // Customers
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal OverdueAmount { get; set; }

        // Inventory
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public decimal TotalStockValue { get; set; }

        // Suppliers
        public int TotalSuppliers { get; set; }
        public decimal SuppliersPending { get; set; }
        public decimal SuppliersPaid { get; set; }

        // Quotations
        public int TotalQuotations { get; set; }
        public int PendingQuotations { get; set; }
        public decimal QuotationsValue { get; set; }

        // Profit (if cost data available)
        public decimal EstimatedProfit { get; set; }
        public decimal ProfitMargin { get; set; }
    }

    // Top Products Model
    public class TopProduct
    {
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    // Recent Bills Model
    public class RecentBill
    {
        public string BillNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal AmountDue { get; set; }
    }

    // Low Stock Model
    public class LowStockItem
    {
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal ReorderLevel { get; set; }
        public string SupplierName { get; set; } = string.Empty;
    }

    // Sales Chart Data
    public class SalesChartData
    {
        public string Period { get; set; } = string.Empty; // Date or Month
        public decimal Sales { get; set; }
        public int BillCount { get; set; }
    }

    // Category Sales Model
    public class CategorySales
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public int ItemCount { get; set; }
        public double Percentage { get; set; }
    }

    // Payment Method Stats
    public class PaymentMethodStats
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}