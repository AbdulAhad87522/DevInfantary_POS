// Models/Staff.cs
namespace HardwareStoreAPI.Models
{
    public class Staff
    {
        public int StaffId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public string? Cnic { get; set; }
        public string? Address { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;  // Joined from lookup
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime? HireDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}