namespace HardwareStoreAPI.Models.DTOs
{
    // Shared payment distribution result (used by both Customer and Supplier bills)
    public class PaymentDistributionResult
    {
        public decimal TotalPayment { get; set; }
        public decimal Applied { get; set; }
        public decimal Remaining { get; set; }
        public List<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
    }

    // Base payment allocation (can be used for both bills and batches)
    public class PaymentAllocation
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public decimal DueBefore { get; set; }
        public decimal PaymentApplied { get; set; }
        public decimal DueAfter { get; set; }
    }
}