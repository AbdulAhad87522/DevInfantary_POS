// Controllers/StaffController.cs
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
    public class StaffController : ControllerBase
    {
        private readonly IStaffService _staffService;
        private readonly ILogger<StaffController> _logger;

        public StaffController(IStaffService staffService, ILogger<StaffController> logger)
        {
            _staffService = staffService;
            _logger = logger;
        }

        /// <summary>Get all staff</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<Staff>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Staff>>>> GetAll([FromQuery] bool includeInactive = false)
        {
            try
            {
                var staff = await _staffService.GetAllStaffAsync(includeInactive);
                return Ok(ApiResponse<List<Staff>>.SuccessResponse(staff, $"Retrieved {staff.Count} staff members"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll");
                return StatusCode(500, ApiResponse<List<Staff>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>Get paginated staff</summary>
        [HttpGet("paginated")]
        [ProducesResponseType(typeof(PaginatedResponse<Staff>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<Staff>>> GetPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _staffService.GetStaffPaginatedAsync(pageNumber, pageSize, includeInactive);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaginated");
                return StatusCode(500, new PaginatedResponse<Staff> { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>Get staff by ID</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<Staff>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Staff>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<Staff>>> GetById(int id)
        {
            try
            {
                var staff = await _staffService.GetStaffByIdAsync(id);
                if (staff == null)
                    return NotFound(ApiResponse<Staff>.ErrorResponse($"Staff with ID {id} not found"));

                return Ok(ApiResponse<Staff>.SuccessResponse(staff));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetById for ID {id}");
                return StatusCode(500, ApiResponse<Staff>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>Get staff by role</summary>
        [HttpGet("role/{roleName}")]
        [ProducesResponseType(typeof(ApiResponse<List<Staff>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Staff>>>> GetByRole(string roleName)
        {
            try
            {
                var staff = await _staffService.GetStaffByRoleAsync(roleName);
                return Ok(ApiResponse<List<Staff>>.SuccessResponse(staff, $"Found {staff.Count} staff with role '{roleName}'"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetByRole for role '{roleName}'");
                return StatusCode(500, ApiResponse<List<Staff>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>Search staff</summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<Staff>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Staff>>>> Search(
            [FromQuery] string term,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                    return BadRequest(ApiResponse<List<Staff>>.ErrorResponse("Search term cannot be empty"));

                var staff = await _staffService.SearchStaffAsync(term, includeInactive);
                return Ok(ApiResponse<List<Staff>>.SuccessResponse(staff, $"Found {staff.Count} staff members"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Search with term '{term}'");
                return StatusCode(500, ApiResponse<List<Staff>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>Create a new staff member</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<Staff>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<Staff>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<Staff>>> Create([FromBody] StaffDto staffDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<Staff>.ErrorResponse("Validation failed", errors));
                }

                var staff = await _staffService.CreateStaffAsync(staffDto);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = staff.StaffId },
                    ApiResponse<Staff>.SuccessResponse(staff, "Staff created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return StatusCode(500, ApiResponse<Staff>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>Update an existing staff member</summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] StaffUpdateDto staffDto)
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

                var success = await _staffService.UpdateStaffAsync(id, staffDto);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Staff with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Staff updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Update for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>Soft delete a staff member</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            try
            {
                var success = await _staffService.DeleteStaffAsync(id);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Staff with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Staff deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Delete for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>Restore a deleted staff member</summary>
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Restore(int id)
        {
            try
            {
                var success = await _staffService.RestoreStaffAsync(id);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Staff with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Staff restored successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Restore for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>Change staff password</summary>
        [HttpPatch("{id}/change-password")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(int id, [FromBody] StaffChangePasswordDto dto)
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

                var success = await _staffService.ChangePasswordAsync(id, dto);
                if (!success)
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Current password is incorrect"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Password changed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ChangePassword for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }
    }
}