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
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierService _supplierService;
        private readonly ILogger<SuppliersController> _logger;

        public SuppliersController(ISupplierService supplierService, ILogger<SuppliersController> logger)
        {
            _supplierService = supplierService;
            _logger = logger;
        }

        /// <summary>
        /// Get all suppliers
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<Supplier>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Supplier>>>> GetAll([FromQuery] bool includeInactive = false)
        {
            try
            {
                var suppliers = await _supplierService.GetAllSuppliersAsync(includeInactive);
                return Ok(ApiResponse<List<Supplier>>.SuccessResponse(suppliers, $"Retrieved {suppliers.Count} suppliers"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll");
                return StatusCode(500, ApiResponse<List<Supplier>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get paginated suppliers
        /// </summary>
        [HttpGet("paginated")]
        [ProducesResponseType(typeof(PaginatedResponse<Supplier>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<Supplier>>> GetPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _supplierService.GetSuppliersPaginatedAsync(pageNumber, pageSize, includeInactive);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaginated");
                return StatusCode(500, new PaginatedResponse<Supplier>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get supplier by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<Supplier>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Supplier>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<Supplier>>> GetById(int id)
        {
            try
            {
                var supplier = await _supplierService.GetSupplierByIdAsync(id);
                if (supplier == null)
                    return NotFound(ApiResponse<Supplier>.ErrorResponse($"Supplier with ID {id} not found"));

                return Ok(ApiResponse<Supplier>.SuccessResponse(supplier));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetById for ID {id}");
                return StatusCode(500, ApiResponse<Supplier>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Create a new supplier
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Supplier>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<Supplier>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<Supplier>>> Create([FromBody] SupplierDto supplierDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<Supplier>.ErrorResponse("Validation failed", errors));
                }

                var supplier = await _supplierService.CreateSupplierAsync(supplierDto);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = supplier.SupplierId },
                    ApiResponse<Supplier>.SuccessResponse(supplier, "Supplier created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return StatusCode(500, ApiResponse<Supplier>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Update an existing supplier
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] SupplierUpdateDto supplierDto)
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

                var success = await _supplierService.UpdateSupplierAsync(id, supplierDto);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Supplier with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Supplier updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Update for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Delete (soft delete) a supplier
        /// </summary>
        [Authorize(Roles ="Admin")]
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            try
            {
                var success = await _supplierService.DeleteSupplierAsync(id);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Supplier with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Supplier deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Delete for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Restore a deleted supplier
        /// </summary>
        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Restore(int id)
        {
            try
            {
                var success = await _supplierService.RestoreSupplierAsync(id);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Supplier with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Supplier restored successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Restore for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Search suppliers by name, contact, or address
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<Supplier>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Supplier>>>> Search(
            [FromQuery] string term,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                    return BadRequest(ApiResponse<List<Supplier>>.ErrorResponse("Search term cannot be empty"));

                var suppliers = await _supplierService.SearchSuppliersAsync(term, includeInactive);
                return Ok(ApiResponse<List<Supplier>>.SuccessResponse(suppliers, $"Found {suppliers.Count} suppliers"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Search with term '{term}'");
                return StatusCode(500, ApiResponse<List<Supplier>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get supplier balance
        /// </summary>
        [HttpGet("{id}/balance")]
        [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<decimal>>> GetBalance(int id)
        {
            try
            {
                var balance = await _supplierService.GetSupplierBalanceAsync(id);
                return Ok(ApiResponse<decimal>.SuccessResponse(balance, "Balance retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetBalance for ID {id}");
                return StatusCode(500, ApiResponse<decimal>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Update supplier balance (add/subtract amount)
        /// </summary>
        [HttpPatch("{id}/balance")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateBalance(int id, [FromBody] decimal amount)
        {
            try
            {
                var success = await _supplierService.UpdateSupplierBalanceAsync(id, amount);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Supplier with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Balance updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in UpdateBalance for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get suppliers with outstanding balance
        /// </summary>
        [HttpGet("with-balance")]
        [ProducesResponseType(typeof(ApiResponse<List<Supplier>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Supplier>>>> GetWithBalance()
        {
            try
            {
                var suppliers = await _supplierService.GetSuppliersWithBalanceAsync();
                return Ok(ApiResponse<List<Supplier>>.SuccessResponse(suppliers, $"Retrieved {suppliers.Count} suppliers with balance"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetWithBalance");
                return StatusCode(500, ApiResponse<List<Supplier>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }
    }
}