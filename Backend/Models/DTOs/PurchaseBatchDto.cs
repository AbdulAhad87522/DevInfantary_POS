using System.ComponentModel.DataAnnotations;

namespace HardwareStoreAPI.Models.DTOs
{
    public class CreatePurchaseBatchDto
    {
        [Required]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Batch name is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Batch name must be between 3 and 200 characters")]
        public string BatchName { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total price must be greater than 0")]
        public decimal TotalPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Paid amount cannot be negative")]
        public decimal Paid { get; set; } = 0;

        public string Status { get; set; } = "Pending";

        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<PurchaseBatchItemDto> Items { get; set; } = new List<PurchaseBatchItemDto>();
    }

    public class UpdatePurchaseBatchDto
    {
        [Required]
        public int SupplierId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string BatchName { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal TotalPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Paid { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";
    }

    public class PurchaseBatchItemDto
    {
        [Required]
        public int VariantId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal QuantityReceived { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cost price must be greater than 0")]
        public decimal CostPrice { get; set; }

        // Optional - calculated on server if not provided
        public decimal? SalePrice { get; set; }
    }

    public class PurchaseBatchSearchDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? SupplierId { get; set; }
        public string? BatchName { get; set; }
        public string? Status { get; set; }
    }
}