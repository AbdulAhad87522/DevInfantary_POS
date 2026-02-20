using System.ComponentModel.DataAnnotations;

namespace HardwareStoreAPI.Models.DTOs
{
    public class CreateProductDto
    {
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        public int? SupplierId { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }

        // Initial variants (at least one required)
        [MinLength(1, ErrorMessage = "At least one product variant is required")]
        public List<CreateProductVariantDto> Variants { get; set; } = new();
    }

    public class UpdateProductDto
    {
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public int? SupplierId { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public class CreateProductVariantDto
    {
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string? ClassType { get; set; }

        [Required(ErrorMessage = "Unit of measure is required")]
        public string UnitOfMeasure { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity in stock is required")]
        [Range(0, 999999, ErrorMessage = "Quantity must be between 0 and 999,999")]
        public decimal QuantityInStock { get; set; }

        [Required(ErrorMessage = "Price per unit is required")]
        [Range(0.01, 999999, ErrorMessage = "Price must be greater than 0")]
        public decimal PricePerUnit { get; set; }

        [Range(0, 999999, ErrorMessage = "Price per length must be between 0 and 999,999")]
        public decimal? PricePerLength { get; set; }

        [Range(0, 999999, ErrorMessage = "Reorder level must be between 0 and 999,999")]
        public decimal ReorderLevel { get; set; } = 10;

        [StringLength(100)]
        public string? Location { get; set; }

        public string? Notes { get; set; }
    }

    public class UpdateProductVariantDto
    {
        [Required]
        public int VariantId { get; set; }

        public string? Size { get; set; }
        public string? Color { get; set; }
        public string? ClassType { get; set; }

        [Required]
        public string UnitOfMeasure { get; set; } = string.Empty;

        [Required]
        [Range(0, 999999)]
        public decimal QuantityInStock { get; set; }

        [Required]
        [Range(0.01, 999999)]
        public decimal PricePerUnit { get; set; }

        [Range(0, 999999)]
        public decimal? PricePerLength { get; set; }

        [Range(0, 999999)]
        public decimal ReorderLevel { get; set; } = 10;

        [StringLength(100)]
        public string? Location { get; set; }

        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }
    }

    public class ProductSearchDto
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public bool? InStock { get; set; }
        public bool? LowStock { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool IncludeInactive { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class CategoryDto
    {
        public int LookupId { get; set; }
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}   