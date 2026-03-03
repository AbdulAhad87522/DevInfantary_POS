using System.ComponentModel.DataAnnotations;

namespace HardwareStoreAPI.Models.DTOs
{
    public class CreateBillDto
    {
        public int? CustomerId { get; set; }

        [Required]
        public DateTime BillDate { get; set; } = DateTime.Now;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Paid amount cannot be negative")]
        public decimal PaidAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<BillItemDto> Items { get; set; } = new List<BillItemDto>();
    }

    public class BillItemDto
    {
        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public string Size { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }
        public string? ClassType { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Unit price cannot be negative")]
        public decimal? UnitPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Discount cannot be negative")]
        public decimal Discount { get; set; } = 0;
    }

    public class BillSearchDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CustomerId { get; set; }
        public string? BillNumber { get; set; }
        public string? PaymentStatus { get; set; }
    }
}