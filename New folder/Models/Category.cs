// Models/Category.cs
namespace HardwareStoreAPI.Models
{
    public class Category
    {
        public int LookupId { get; set; }
        public string Type { get; set; } = "category";
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
    }
}