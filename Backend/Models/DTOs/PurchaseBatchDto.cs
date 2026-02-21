using System.ComponentModel.DataAnnotations;

namespace HardwareStoreAPI.Models.DTOs
{
    public class CreatePurchaseBatchDto
    {
        [Required(ErrorMessage = "Batch name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Batch name must be between 2 and 100 characters")]
        public string BatchName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Supplier ID is required")]
        public int SupplierId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Total price cannot be negative")]
        public decimal TotalPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Paid amount cannot be negative")]
        public decimal Paid { get; set; } = 0;

        [Required(ErrorMessage = "Status is required")]
        [RegularExpression("^(Pending|Completed|Partial)$", ErrorMessage = "Status must be Pending, Completed, or Partial")]
        public string Status { get; set; } = "Pending";

        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<CreatePurchaseBatchItemDto> Items { get; set; } = new List<CreatePurchaseBatchItemDto>();
    }

    public class UpdatePurchaseBatchDto
    {
        [Required(ErrorMessage = "Batch name is required")]
        [StringLength(100, MinimumLength = 2)]
        public string BatchName { get; set; } = string.Empty;

        [Required]
        public int SupplierId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Paid { get; set; }

        [Required]
        [RegularExpression("^(Pending|Completed|Partial)$")]
        public string Status { get; set; } = "Pending";
    }

    public class CreatePurchaseBatchItemDto
    {
        [Required]
        public int VariantId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal QuantityReceived { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cost price must be greater than 0")]
        public decimal CostPrice { get; set; }

        // Optional - if not provided, will be calculated as QuantityReceived * CostPrice
        public decimal? LineTotal { get; set; }

        // For display/reference
        public string? ProductName { get; set; }
        public string? Size { get; set; }
        public string? ClassType { get; set; }
        public decimal? SalePrice { get; set; }
    }

    public class UpdatePurchaseBatchItemDto
    {
        [Required]
        public int PurchaseBatchItemId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal QuantityReceived { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal CostPrice { get; set; }

        public decimal? LineTotal { get; set; }
    }

    public class PurchaseBatchSearchDto
    {
        public string? SearchTerm { get; set; }
        public int? SupplierId { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? MinTotal { get; set; }
        public decimal? MaxTotal { get; set; }
        public bool? HasOutstanding { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class VariantForSelectionDto
    {
        public int VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Size { get; set; }
        public string? ClassType { get; set; }
        public decimal SalePrice { get; set; }
        public decimal QuantityInStock { get; set; }
    }

    public class BatchPaymentDto
    {
        [Required]
        public int BatchId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than 0")]
        public decimal Amount { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }
    }
}