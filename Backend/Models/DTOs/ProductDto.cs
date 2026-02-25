using System.ComponentModel.DataAnnotations;

namespace HardwareStoreAPI.Models.DTOs
{
    // ==================== PRODUCT DTOs ====================

    /// <summary>
    /// DTO for creating a new product (without variants)
    /// </summary>
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
    }

    /// <summary>
    /// DTO for updating an existing product
    /// </summary>
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
    }

    // ==================== PRODUCT VARIANT DTOs ====================

    /// <summary>
    /// DTO for creating a new product variant
    /// </summary>
    public class CreateProductVariantDto
    {
        [StringLength(100, ErrorMessage = "Size cannot exceed 100 characters")]
        public string? Size { get; set; }

        [StringLength(100, ErrorMessage = "Class type cannot exceed 100 characters")]
        public string? ClassType { get; set; }

        [Required(ErrorMessage = "Unit of measure is required")]
        [StringLength(50, ErrorMessage = "Unit of measure cannot exceed 50 characters")]
        public string UnitOfMeasure { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity in stock is required")]
        [Range(0, 999999, ErrorMessage = "Quantity must be between 0 and 999,999")]
        public decimal QuantityInStock { get; set; }

        [Required(ErrorMessage = "Price per unit is required")]
        [Range(0.01, 999999, ErrorMessage = "Price must be greater than 0 and less than 999,999")]
        public decimal PricePerUnit { get; set; }

        [Range(0, 999999, ErrorMessage = "Price per length must be between 0 and 999,999")]
        public decimal? PricePerLength { get; set; }

        [Range(0, 999999, ErrorMessage = "Length in feet must be between 0 and 999,999")]
        public decimal? LengthInFeet { get; set; }

        [Range(0, 999999, ErrorMessage = "Reorder level must be between 0 and 999,999")]
        public decimal ReorderLevel { get; set; } = 10;
    }

    /// <summary>
    /// DTO for updating an existing product variant
    /// </summary>
    public class UpdateProductVariantDto
    {
        [Required]
        public int VariantId { get; set; }

        [StringLength(100, ErrorMessage = "Size cannot exceed 100 characters")]
        public string? Size { get; set; }

        [StringLength(100, ErrorMessage = "Class type cannot exceed 100 characters")]
        public string? ClassType { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Unit of measure cannot exceed 50 characters")]
        public string UnitOfMeasure { get; set; } = string.Empty;

        [Required]
        [Range(0, 999999, ErrorMessage = "Quantity must be between 0 and 999,999")]
        public decimal QuantityInStock { get; set; }

        [Required]
        [Range(0.01, 999999, ErrorMessage = "Price must be greater than 0 and less than 999,999")]
        public decimal PricePerUnit { get; set; }

        [Range(0, 999999, ErrorMessage = "Price per length must be between 0 and 999,999")]
        public decimal? PricePerLength { get; set; }

        [Range(0, 999999, ErrorMessage = "Length in feet must be between 0 and 999,999")]
        public decimal? LengthInFeet { get; set; }

        [Range(0, 999999, ErrorMessage = "Reorder level must be between 0 and 999,999")]
        public decimal ReorderLevel { get; set; } = 10;

        public bool IsActive { get; set; } = true;
    }

    // ==================== SEARCH & FILTER DTOs ====================

    /// <summary>
    /// DTO for advanced product search with filters
    /// </summary>
    public class ProductSearchDto
    {
        /// <summary>
        /// Search term for product name or description
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filter by category ID
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Filter by supplier ID
        /// </summary>
        public int? SupplierId { get; set; }

        /// <summary>
        /// Filter products that have stock
        /// </summary>
        public bool? InStock { get; set; }

        /// <summary>
        /// Filter products with low stock (quantity <= reorder level)
        /// </summary>
        public bool? LowStock { get; set; }

        /// <summary>
        /// Minimum price filter
        /// </summary>
        [Range(0, 999999)]
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// Maximum price filter
        /// </summary>
        [Range(0, 999999)]
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// Include inactive products in results
        /// </summary>
        public bool IncludeInactive { get; set; } = false;

        /// <summary>
        /// Page number for pagination (starts at 1)
        /// </summary>
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
    }

    // ==================== CATEGORY DTOs ====================

    /// <summary>
    /// DTO for product category (from lookup table)
    /// </summary>
    public class CategoryDto
    {
        public int LookupId { get; set; }

        [Required]
        [StringLength(200)]
        public string Value { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}