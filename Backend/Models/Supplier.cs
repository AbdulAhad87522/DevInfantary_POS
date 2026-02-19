namespace HardwareStoreAPI.Models
{
    public class Supplier
    {
        public int SupplierId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Contact { get; set; }
        public string? Address { get; set; }
        public decimal AccountBalance { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Notes { get; set; }
    }
}