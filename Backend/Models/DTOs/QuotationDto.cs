using System.ComponentModel.DataAnnotations;

namespace HardwareStoreAPI.Models.DTOs
{
    public class CreateQuotationDto
    {
        public int? CustomerId { get; set; }

        [Required]
        public DateTime QuotationDate { get; set; } = DateTime.Now;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
        public decimal TotalAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public DateTime? ValidUntil { get; set; }

        public string? Notes { get; set; }

        public string? TermsConditions { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<QuotationItemDto> Items { get; set; } = new List<QuotationItemDto>();
    }

    public class QuotationItemDto
    {
        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public string Size { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Unit price cannot be negative")]
        public decimal? UnitPrice { get; set; }

        public string? Notes { get; set; }
    }

    public class QuotationSearchDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CustomerId { get; set; }
        public string? QuotationNumber { get; set; }
        public string? Status { get; set; }
    }

    public class ConvertQuotationToBillDto
    {
        [Required]
        public int QuotationId { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Paid amount cannot be negative")]
        public decimal PaidAmount { get; set; }
    }
}