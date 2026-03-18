using System.ComponentModel.DataAnnotations;

namespace HardwareStoreAPI.Models.DTOs
{
    // DTO for stock requirement item
    public class StockRequirementItemDto
    {
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string? ClassType { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal RequiredQuantity { get; set; }  // Prefilled with reorder level
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal EstimatedCost { get; set; }  // RequiredQuantity × CostPrice
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? Category { get; set; }
    }

    // DTO for full stock requirement report
    public class StockRequirementReportDto
    {
        public DateTime GeneratedAt { get; set; }
        public int TotalItemsLowStock { get; set; }
        public decimal TotalEstimatedCost { get; set; }
        public List<StockRequirementItemDto> Items { get; set; } = new List<StockRequirementItemDto>();
    }

    // DTO for generating requirement with filters
    public class GenerateRequirementDto
    {
        // Optional filters
        public int? SupplierId { get; set; }
        public int? CategoryId { get; set; }

        [Range(0, 100)]
        public decimal? StockThresholdPercentage { get; set; }  // e.g., 20 = show items below 20% of reorder level
    }

    // DTO for creating purchase order from requirement
    public class CreatePurchaseFromRequirementDto
    {
        [Required]
        public int SupplierId { get; set; }

        [Required]
        [StringLength(200)]
        public string BatchName { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public List<RequirementItemForPurchaseDto> Items { get; set; } = new List<RequirementItemForPurchaseDto>();
    }

    public class RequirementItemForPurchaseDto
    {
        [Required]
        public int VariantId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal CostPrice { get; set; }
    }
}