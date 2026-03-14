using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HardwareStoreAPI.Models.DTOs
{
    /// <summary>
    /// Complete dashboard data container - all data in one response
    /// </summary>
    public class DashboardData
    {
        public DashboardStats Stats { get; set; } = new();
        public List<TopProduct> TopProducts { get; set; } = new();
        public List<RecentBill> RecentBills { get; set; } = new();
        public List<LowStockItem> LowStockItems { get; set; } = new();
        public List<SalesChartData> SalesChartData { get; set; } = new();
        public List<CategorySales> CategorySales { get; set; } = new();
        public List<PaymentMethodStats> PaymentMethodStats { get; set; } = new();
    }

    /// <summary>
    /// Dashboard filter options
    /// </summary>
    public class DashboardFilters
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [Range(1, 100)]
        public int TopProductsLimit { get; set; } = 10;

        [Range(1, 100)]
        public int RecentBillsLimit { get; set; } = 10;

        [Range(1, 100)]
        public int LowStockLimit { get; set; } = 10;

        [Range(7, 365)]
        public int SalesTrendDays { get; set; } = 30;

        public bool IncludeInactive { get; set; } = false;
    }

    /// <summary>
    /// Dashboard export request
    /// </summary>
    public class DashboardExportRequest
    {
        [Required]
        public string Format { get; set; } = "CSV"; // CSV, JSON, PDF

        public bool IncludeStats { get; set; } = true;
        public bool IncludeTopProducts { get; set; } = true;
        public bool IncludeRecentBills { get; set; } = true;
        public bool IncludeLowStock { get; set; } = true;
        public bool IncludeSalesTrend { get; set; } = true;
        public bool IncludeCategorySales { get; set; } = true;
        public bool IncludePaymentStats { get; set; } = true;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    /// Quick stats summary (for mobile/compact views)
    /// </summary>
    public class QuickStats
    {
        public decimal TodayRevenue { get; set; }
        public int TodayBills { get; set; }
        public decimal PendingAmount { get; set; }
        public int LowStockCount { get; set; }
        public int PendingBillsCount { get; set; }
    }

    /// <summary>
    /// Revenue breakdown by period
    /// </summary>
    public class RevenueSummary
    {
        public decimal Today { get; set; }
        public decimal Yesterday { get; set; }
        public decimal ThisWeek { get; set; }
        public decimal LastWeek { get; set; }
        public decimal ThisMonth { get; set; }
        public decimal LastMonth { get; set; }
        public decimal ThisYear { get; set; }
        public decimal AllTime { get; set; }

        // Growth percentages
        public decimal DailyGrowth { get; set; }
        public decimal WeeklyGrowth { get; set; }
        public decimal MonthlyGrowth { get; set; }
    }

    /// <summary>
    /// Inventory summary
    /// </summary>
    public class InventorySummary
    {
        public int TotalProducts { get; set; }
        public int TotalVariants { get; set; }
        public int ActiveProducts { get; set; }
        public int InStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public decimal TotalStockValue { get; set; }
        public decimal AverageProductValue { get; set; }
    }

    /// <summary>
    /// Customer insights
    /// </summary>
    public class CustomerInsights
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int CustomersWithPendingBills { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal AverageOutstanding { get; set; }
        public TopCustomer? TopCustomer { get; set; }
    }

    public class TopCustomer
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalPurchases { get; set; }
        public int TotalBills { get; set; }
    }

    /// <summary>
    /// Supplier insights
    /// </summary>
    public class SupplierInsights
    {
        public int TotalSuppliers { get; set; }
        public int ActiveSuppliers { get; set; }
        public decimal TotalDue { get; set; }
        public decimal TotalPaid { get; set; }
        public int TotalBatches { get; set; }
        public TopSupplier? TopSupplier { get; set; }
    }

    public class TopSupplier
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal TotalPurchases { get; set; }
        public int TotalBatches { get; set; }
    }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public class PerformanceMetrics
    {
        public decimal AverageBillValue { get; set; }
        public decimal AverageDailyRevenue { get; set; }
        public decimal ConversionRate { get; set; } // Quotations to Bills
        public decimal ReturnRate { get; set; } // Returns vs Sales
        public int AverageBillsPerDay { get; set; }
        public decimal InventoryTurnoverRate { get; set; }
    }

    /// <summary>
    /// Alerts and notifications
    /// </summary>
    public class DashboardAlerts
    {
        public List<StockAlert> StockAlerts { get; set; } = new();
        public List<PaymentAlert> PaymentAlerts { get; set; } = new();
        public List<SystemAlert> SystemAlerts { get; set; } = new();
    }

    public class StockAlert
    {
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal ReorderLevel { get; set; }
        public string Severity { get; set; } = string.Empty; // Critical, Warning, Info
    }

    public class PaymentAlert
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int DaysOverdue { get; set; }
        public string Severity { get; set; } = string.Empty;
    }

    public class SystemAlert
    {
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Info, Warning, Error
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Period comparison (for charts)
    /// </summary>
    public class PeriodComparison
    {
        public string PeriodName { get; set; } = string.Empty;
        public decimal CurrentPeriod { get; set; }
        public decimal PreviousPeriod { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePercentage { get; set; }
    }

    /// <summary>
    /// Sales by time of day
    /// </summary>
    public class HourlySales
    {
        public int Hour { get; set; }
        public string TimeRange { get; set; } = string.Empty; // "9 AM - 10 AM"
        public decimal Sales { get; set; }
        public int BillCount { get; set; }
    }

    /// <summary>
    /// Sales by day of week
    /// </summary>
    public class WeeklySales
    {
        public string DayName { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public int BillCount { get; set; }
    }
}