using System;

namespace HardwareStoreAPI.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; } // From lookup table
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Notes { get; set; }

        // Navigation property for variants
        public List<ProductVariant> Variants { get; set; } = new();
    }

    public class ProductVariant
    {
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string? ClassType { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal QuantityInStock { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal? PricePerLength { get; set; }
        public decimal? LengthInFeet { get; set; }
        public decimal ReorderLevel { get; set; } = 10;
        public string? Location { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class ProductWithStock
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public int VariantCount { get; set; }
        public decimal TotalStock { get; set; }
        public decimal TotalValue { get; set; }
        public bool IsActive { get; set; }
    }
}