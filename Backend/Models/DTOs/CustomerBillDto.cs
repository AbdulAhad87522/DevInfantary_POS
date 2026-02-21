using System.ComponentModel.DataAnnotations;

namespace HardwareStoreAPI.Models.DTOs
{
    public class AddCustomerPaymentDto
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than 0")]
        public decimal PaymentAmount { get; set; }

        public string? Remarks { get; set; }
    }

    public class CustomerBillSearchDto
    {
        public string? CustomerName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? PaymentStatus { get; set; }
    }

    public class PaymentDistributionResult
    {
        public decimal TotalPayment { get; set; }
        public decimal Applied { get; set; }
        public decimal Remaining { get; set; }
        public List<BillPaymentAllocation> Allocations { get; set; } = new List<BillPaymentAllocation>();
    }

    public class BillPaymentAllocation
    {
        public int BillId { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public decimal BillDueBefore { get; set; }
        public decimal PaymentApplied { get; set; }
        public decimal BillDueAfter { get; set; }
    }
}