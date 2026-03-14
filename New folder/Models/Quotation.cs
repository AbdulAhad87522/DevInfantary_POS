namespace HardwareStoreAPI.Models
{
    public class Quotation
    {
        public int QuotationId { get; set; }
        public string QuotationNumber { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public int StaffId { get; set; }
        public DateTime QuotationDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public int StatusId { get; set; }
        public int? ConvertedBillId { get; set; }
        public string? Notes { get; set; }
        public string? TermsConditions { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public string? CustomerName { get; set; }
        public string? CustomerContact { get; set; }
        public string? StaffName { get; set; }
        public string? Status { get; set; }
        public List<QuotationItem> Items { get; set; } = new List<QuotationItem>();
    }

    public class QuotationItem
    {
        public int QuotationItemId { get; set; }
        public int QuotationId { get; set; }
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
        public string? ClassType { get; set; }
        public decimal? AvailableStock { get; set; }
        public string? SupplierName { get; set; }
        public string? Category { get; set; }
    }
}