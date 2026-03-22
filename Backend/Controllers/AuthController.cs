using HardwareStoreAPI.Models.DTOs;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HardwareStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Login with username and password
        /// </summary>
        [HttpPost("login")]
            [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid request data"
                });
            }

            var response = await _authService.LoginAsync(loginDto);

            if (!response.Success)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }

        [HttpGet("test-verify")]
        [AllowAnonymous]
        public IActionResult TestVerify()
        {
            var password = "ahmed123";
            var hash = "$2b$10$PASTE_AHMED_FULL_HASH_HERE";

            var result = BCrypt.Net.BCrypt.Verify(password, hash);
            return Ok(new
            {
                verified = result,
                hashPrefix = hash.Substring(0, 7)
            });
        }

        [HttpGet("debug-conn")]
        [AllowAnonymous]
        public IActionResult DebugConn([FromServices] IConfiguration config)
        {
            var conn = config.GetConnectionString("DefaultConnection");
            return Ok(new { host = conn?.Split(';')[0] });
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid request data"
                });
            }

            var response = await _authService.RegisterAsync(registerDto);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Get current authenticated user information
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserInfo>> GetCurrentUser()
        {
            // ✅ Try multiple claim types for robustness
            var staffIdClaim = User.FindFirst("StaffId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(staffIdClaim) || !int.TryParse(staffIdClaim, out int staffId))
            {
                return Unauthorized(new { message = "Invalid token or user not found" });
            }

            var user = await _authService.GetUserByIdAsync(staffId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new UserInfo
            {
                StaffId = user.StaffId,
                Username = user.Username,
                Name = user.Name,
                Email = user.Email ?? "",
                Role = user.RoleName ?? "User"
            });
        }
    }
}