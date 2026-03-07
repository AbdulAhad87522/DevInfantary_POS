using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HardwareStoreAPI.Controllers
{
    public class BaseController : ControllerBase
    {
        /// <summary>
        /// Get the current staff ID from JWT token
        /// </summary>
        protected int GetCurrentStaffId()
        {
            if (User.Identity?.IsAuthenticated != true)
                return 1; // Default to 1 if not authenticated

            // Try multiple claim types
            var staffIdClaim = User.FindFirst("StaffId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (int.TryParse(staffIdClaim, out int staffId))
                return staffId;

            return 1; // Fallback to 1
        }

        /// <summary>
        /// Get the current staff name from JWT token
        /// </summary>
        protected string GetCurrentStaffName()
        {
            if (User.Identity?.IsAuthenticated != true)
                return "System";

            return User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.Identity.Name
                ?? "System";
        }

        /// <summary>
        /// Get the current staff role from JWT token
        /// </summary>
        protected string GetCurrentStaffRole()
        {
            if (User.Identity?.IsAuthenticated != true)
                return "User";

            return User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        }
    }
}