using System;

namespace HardwareStoreAPI.Models
{
    public class PurchaseBatch
    {
        public int BatchId { get; set; }
        public int SupplierId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public decimal Paid { get; set; }
        public string Status { get; set; } = "Pending"; // "Pending", "Completed", "Partial"
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // For display purposes
        public string? SupplierName { get; set; }
        public decimal Remaining => TotalPrice - Paid;
        public List<PurchaseBatchItem> Items { get; set; } = new List<PurchaseBatchItem>();
    }

    public class PurchaseBatchItem
    {
        public int PurchaseBatchItemId { get; set; }
        public int PurchaseBatchId { get; set; }
        public int VariantId { get; set; }
        public decimal QuantityReceived { get; set; }
        public decimal CostPrice { get; set; }
        public decimal LineTotal { get; set; }
        public DateTime CreatedAt { get; set; }

        // For display purposes
        public string? ProductName { get; set; }
        public string? Size { get; set; }
        public string? ClassType { get; set; }
        public decimal SalePrice { get; set; } // price_per_unit from variants
    }

    public class PurchaseBatchSummary
    {
        public int BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}