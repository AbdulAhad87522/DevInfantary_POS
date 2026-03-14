namespace HardwareStoreAPI.Models
{
    public class SupplierBillSummary
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
        public string Status { get; set; } = string.Empty;
        public int BatchCount { get; set; }
    }

    public class SupplierBatchDetail
    {
        public int BatchId { get; set; }
        public int SupplierId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<SupplierBatchItem> Items { get; set; } = new List<SupplierBatchItem>();
    }

    public class SupplierBatchItem
    {
        public int PurchaseBatchItemId { get; set; }
        public int PurchaseBatchId { get; set; }
        public int VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string? ClassType { get; set; }
        public decimal QuantityReceived { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal LineTotal { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SupplierPaymentRecord
    {
        public int PaymentId { get; set; }
        public int SupplierId { get; set; }
        public int BatchId { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? SupplierName { get; set; }
        public string? BatchName { get; set; }
    }
}