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

    /// <summary>
    /// DTO for updating a purchase batch/supplier bill
    /// </summary>
    public class UpdateSupplierBatchDto
    {
        [Required]
        public int BatchId { get; set; }

        [Required]
        [StringLength(200)]
        public string BatchName { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public List<UpdateBatchItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO for updating a batch item
    /// </summary>
    public class UpdateBatchItemDto
    {
        public int? PurchaseBatchItemId { get; set; }  // null for new items

        [Required]
        public int VariantId { get; set; }

        [Required]
        [Range(0.01, 999999)]
        public decimal QuantityReceived { get; set; }

        [Required]
        [Range(0.01, 999999)]
        public decimal CostPrice { get; set; }

        public bool IsDeleted { get; set; } = false;  // Mark item for deletion
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