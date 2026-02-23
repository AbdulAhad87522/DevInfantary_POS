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

    // ✅ REMOVED - Use shared PaymentDistributionResult from CommonDto.cs

    public class BillPaymentAllocation : PaymentAllocation
    {
        public int BillId
        {
            get => Id;
            set => Id = value;
        }

        public string BillNumber
        {
            get => ReferenceNumber;
            set => ReferenceNumber = value;
        }

        public decimal BillDueBefore
        {
            get => DueBefore;
            set => DueBefore = value;
        }

        public decimal BillDueAfter
        {
            get => DueAfter;
            set => DueAfter = value;
        }
    }
}