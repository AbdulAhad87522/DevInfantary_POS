using System.ComponentModel.DataAnnotations;

namespace HardwareStoreAPI.Models.DTOs
{
    public class AddSupplierPaymentDto
    {
        [Required]
        public int SupplierId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than 0")]
        public decimal PaymentAmount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Remarks { get; set; }
    }

    public class SupplierBillSearchDto
    {
        public string? SupplierName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
    }

    // ✅ REMOVED - Use shared PaymentDistributionResult from CommonDto.cs

    public class BatchPaymentAllocation : PaymentAllocation
    {
        public int BatchId
        {
            get => Id;
            set => Id = value;
        }

        public string BatchName
        {
            get => ReferenceNumber;
            set => ReferenceNumber = value;
        }

        public decimal BatchDueBefore
        {
            get => DueBefore;
            set => DueBefore = value;
        }

        public decimal BatchDueAfter
        {
            get => DueAfter;
            set => DueAfter = value;
        }
    }
}