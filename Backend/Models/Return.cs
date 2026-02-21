namespace HardwareStoreAPI.Models
{
    public class Return
    {
        public int ReturnId { get; set; }
        public int? BillId { get; set; }
        public int? CustomerId { get; set; }
        public DateTime ReturnDate { get; set; }
        public decimal RefundAmount { get; set; }
        public int StatusId { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public string? BillNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? Status { get; set; }
        public List<ReturnItem> Items { get; set; } = new List<ReturnItem>();
    }

    public class ReturnItem
    {
        public int ReturnItemId { get; set; }
        public int ReturnId { get; set; }
        public int VariantId { get; set; }
        public decimal Quantity { get; set; }
        public string? ConditionNote { get; set; }

        // Additional info
        public string? ProductName { get; set; }
        public string? Size { get; set; }
        public string? UnitOfMeasure { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? LineTotal { get; set; }
    }
}