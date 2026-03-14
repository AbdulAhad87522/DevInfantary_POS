// Models/DTOs/CategoryDto.cs
using System.ComponentModel.DataAnnotations;

namespace HardwareStoreAPI.Models.DTOs
{
    public class CategoryDto1
    {
        [Required(ErrorMessage = "Value is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Value must be between 2 and 100 characters")]
        public string Value { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public int? DisplayOrder { get; set; }
    }

    public class CategoryUpdateDto
    {
        [Required(ErrorMessage = "Value is required")]
        [StringLength(100, MinimumLength = 2)]
        public string Value { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int? DisplayOrder { get; set; }
    }
}