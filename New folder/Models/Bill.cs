namespace HardwareStoreAPI.Models
{
    public class Bill
    {
        public int BillId { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public int StaffId { get; set; }
        public DateTime BillDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountDue { get; set; }
        public int PaymentStatusId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public string? CustomerName { get; set; }
        public string? PaymentStatus { get; set; }
        public List<BillItem> Items { get; set; } = new List<BillItem>();
    }

    public class BillItem
    {
        public int BillItemId { get; set; }
        public int BillId { get; set; }
        public int ProductId { get; set; }
        public int VariantId { get; set; }
        public decimal Quantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string? Notes { get; set; }

        // Additional info
        public string? ProductName { get; set; }
        public string? Size { get; set; }
    }
}