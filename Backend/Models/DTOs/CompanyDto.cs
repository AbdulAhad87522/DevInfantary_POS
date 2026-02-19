using System.ComponentModel.DataAnnotations;

namespace HardwareStoreAPI.Models.DTOs
{
    public class CompanyDto
    {
        [Required]
        [MaxLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Contact { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }
    }
}
