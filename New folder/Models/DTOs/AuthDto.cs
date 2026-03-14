using System.ComponentModel.DataAnnotations;

namespace HardwareStoreAPI.Models.DTOs
{
    public class LoginDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        [Required]
        [MinLength(3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;  // Changed from FullName

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Contact { get; set; }
        public string? Cnic { get; set; }
        public string? Address { get; set; }

        public string Role { get; set; } = "User";  // Will be converted to role_id
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public UserInfo? User { get; set; }
    }

    public class UserInfo
    {
        public int StaffId { get; set; }  // Changed from UserId
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;  // Changed from FullName
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}