using System.ComponentModel.DataAnnotations;

namespace HardwareStoreAPI.Models.DTOs
{
    public class ProcessReturnDto
    {
        [Required]
        public int BillId { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Refund amount cannot be negative")]
        public decimal RefundAmount { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }

        [Required]
        public bool RestoreStock { get; set; } = true;

        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<ReturnItemDto> Items { get; set; } = new List<ReturnItemDto>();
    }

    public class ReturnItemDto
    {
        [Required]
        public int VariantId { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        public string Size { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Unit price cannot be negative")]
        public decimal UnitPrice { get; set; }

        public decimal LineTotal { get; set; }

        public decimal MaxQuantity { get; set; }
    }

    public class ReturnSearchDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CustomerId { get; set; }
        public int? BillId { get; set; }
        public string? Status { get; set; }
    }

    public class BillForReturnDto
    {
        public int BillId { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime BillDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<BillItemForReturnDto> Items { get; set; } = new List<BillItemForReturnDto>();
    }

    public class BillItemForReturnDto
    {
        public int BillItemId { get; set; }
        public int BillId { get; set; }
        public int ProductId { get; set; }
        public int VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string? Notes { get; set; }
    }
}