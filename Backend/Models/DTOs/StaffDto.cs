// Models/DTOs/StaffDto.cs
using System.ComponentModel.DataAnnotations;

namespace HardwareStoreAPI.Models.DTOs
{
    public class StaffDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid contact number")]
        [StringLength(15, ErrorMessage = "Contact cannot exceed 15 characters")]
        public string? Contact { get; set; }

        [StringLength(13, MinimumLength = 13, ErrorMessage = "CNIC must be exactly 13 digits")]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "CNIC must contain exactly 13 digits")]
        public string? Cnic { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [RegularExpression("^(Admin|Manager|Cashier|Inventory)$",
            ErrorMessage = "Role must be: Admin, Manager, Cashier, or Inventory")]
        public string RoleName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        public DateTime? HireDate { get; set; }
    }

    public class StaffUpdateDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [Phone]
        [StringLength(15)]
        public string? Contact { get; set; }

        [StringLength(13, MinimumLength = 13)]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "CNIC must contain exactly 13 digits")]
        public string? Cnic { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [RegularExpression("^(Admin|Manager|Cashier|Inventory)$",
            ErrorMessage = "Role must be: Admin, Manager, Cashier, or Inventory")]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(50, MinimumLength = 3)]
        public string? Username { get; set; }

        public DateTime? HireDate { get; set; }
    }

    public class StaffChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;
    }
}