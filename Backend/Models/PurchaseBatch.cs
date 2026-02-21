namespace HardwareStoreAPI.Models
{
    public class PurchaseBatch
    {
        public int BatchId { get; set; }
        public int SupplierId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Partial, Completed
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public string? SupplierName { get; set; }
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

        // Additional info
        public string? ProductName { get; set; }
        public string? Size { get; set; }
        public string? ClassType { get; set; }
        public decimal? SalePrice { get; set; }
    }

    public class ProductVariantForBatch
    {
        public int VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string? ClassType { get; set; }
        public decimal SalePrice { get; set; }
        public decimal QuantityInStock { get; set; }
    }
}