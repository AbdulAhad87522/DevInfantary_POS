namespace HardwareStoreAPI.Models
{
    public class CustomerBillSummary
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
        public int BillCount { get; set; }
    }

    public class CustomerBillDetail
    {
        public int BillId { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountDue { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public List<BillItemDetail> Items { get; set; } = new List<BillItemDetail>();
    }

    public class BillItemDetail
    {
        public int BillItemId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class CustomerPaymentRecord
    {
        public int RecordId { get; set; }
        public int CustomerId { get; set; }
        public int? BillId { get; set; }
        public DateTime Date { get; set; }
        public decimal Payment { get; set; }
        public string? Remarks { get; set; }
        public string? BillNumber { get; set; }
    }
}