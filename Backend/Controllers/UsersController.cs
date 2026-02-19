using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HardwareStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize] // Require authentication for all endpoints by default
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user and get token
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<UserLoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<UserLoginResponse>>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<object>.ErrorResponse("Validation failed", errors));
                }

                var result = await _userService.LoginAsync(loginDto);

                if (result == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid username or password"));
                }

                return Ok(ApiResponse<UserLoginResponse>.SuccessResponse(result, "Login successful"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get all users (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(ApiResponse<List<User>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<User>>>> GetAll([FromQuery] bool includeInactive = false)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync(includeInactive);
                return Ok(ApiResponse<List<User>>.SuccessResponse(users, $"Retrieved {users.Count} users"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll");
                return StatusCode(500, ApiResponse<List<User>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get paginated users (Admin only)
        /// </summary>
        [HttpGet("paginated")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(PaginatedResponse<User>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<User>>> GetPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _userService.GetUsersPaginatedAsync(pageNumber, pageSize, includeInactive);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaginated");
                return StatusCode(500, new PaginatedResponse<User>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "admin,manager")]
        [ProducesResponseType(typeof(ApiResponse<User>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<User>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<User>>> GetById(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound(ApiResponse<User>.ErrorResponse($"User with ID {id} not found"));

                return Ok(ApiResponse<User>.SuccessResponse(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetById for ID {id}");
                return StatusCode(500, ApiResponse<User>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(ApiResponse<User>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<User>>> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst("staff_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ApiResponse<User>.ErrorResponse("Invalid user token"));
                }

                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                    return NotFound(ApiResponse<User>.ErrorResponse("User not found"));

                return Ok(ApiResponse<User>.SuccessResponse(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProfile");
                return StatusCode(500, ApiResponse<User>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Create a new user (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(ApiResponse<User>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<User>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<User>>> Create([FromBody] CreateUserDto userDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<User>.ErrorResponse("Validation failed", errors));
                }

                var user = await _userService.CreateUserAsync(userDto);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = user.StaffId },
                    ApiResponse<User>.SuccessResponse(user, "User created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<User>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return StatusCode(500, ApiResponse<User>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] UpdateUserDto userDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Validation failed", errors));
                }

                var success = await _userService.UpdateUserAsync(id, userDto);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"User with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "User updated successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Update for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Delete (soft delete) a user (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            try
            {
                // Prevent deleting yourself
                var currentUserIdClaim = User.FindFirst("staff_id")?.Value;
                if (!string.IsNullOrEmpty(currentUserIdClaim) && int.Parse(currentUserIdClaim) == id)
                {
                    return BadRequest(ApiResponse<bool>.ErrorResponse("You cannot delete your own account"));
                }

                var success = await _userService.DeleteUserAsync(id);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"User with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "User deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Delete for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Restore a deleted user (Admin only)
        /// </summary>
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Restore(int id)
        {
            try
            {
                var success = await _userService.RestoreUserAsync(id);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"User with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "User restored successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Restore for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Validation failed", errors));
                }

                var userIdClaim = User.FindFirst("staff_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ApiResponse<bool>.ErrorResponse("Invalid user token"));
                }

                var success = await _userService.ChangePasswordAsync(userId, changePasswordDto);
                if (!success)
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Current password is incorrect"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Password changed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChangePassword");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Search users by name, username, email, or phone
        /// </summary>
        [HttpGet("search")]
        [Authorize(Roles = "admin,manager")]
        [ProducesResponseType(typeof(ApiResponse<List<User>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<User>>>> Search(
            [FromQuery] string term,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                    return BadRequest(ApiResponse<List<User>>.ErrorResponse("Search term cannot be empty"));

                var users = await _userService.SearchUsersAsync(term, includeInactive);
                return Ok(ApiResponse<List<User>>.SuccessResponse(users, $"Found {users.Count} users"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Search with term '{term}'");
                return StatusCode(500, ApiResponse<List<User>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get users by role
        /// </summary>
        [HttpGet("role/{role}")]
        [Authorize(Roles = "admin,manager")]
        [ProducesResponseType(typeof(ApiResponse<List<User>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<User>>>> GetByRole(string role)
        {
            try
            {
                var users = await _userService.GetUsersByRoleAsync(role);
                return Ok(ApiResponse<List<User>>.SuccessResponse(users, $"Found {users.Count} users with role '{role}'"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetByRole for role '{role}'");
                return StatusCode(500, ApiResponse<List<User>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Update user status (activate/deactivate)
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateStatus(int id, [FromBody] UpdateUserStatusDto statusDto)
        {
            try
            {
                // Prevent deactivating yourself
                if (!statusDto.IsActive)
                {
                    var currentUserIdClaim = User.FindFirst("staff_id")?.Value;
                    if (!string.IsNullOrEmpty(currentUserIdClaim) && int.Parse(currentUserIdClaim) == id)
                    {
                        return BadRequest(ApiResponse<bool>.ErrorResponse("You cannot deactivate your own account"));
                    }
                }

                var success = await _userService.UpdateUserStatusAsync(id, statusDto.IsActive);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"User with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, $"User {(statusDto.IsActive ? "activated" : "deactivated")} successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in UpdateStatus for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }
    }
}