using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HardwareStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        /// <summary>
        /// Get all customers
        /// </summary>
        /// <param name="includeInactive">Include inactive customers</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<Customer>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Customer>>>> GetAll([FromQuery] bool includeInactive = false)
        {
            try
            {
                var customers = await _customerService.GetAllCustomersAsync(includeInactive);
                return Ok(ApiResponse<List<Customer>>.SuccessResponse(customers, $"Retrieved {customers.Count} customers"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll");
                return StatusCode(500, ApiResponse<List<Customer>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get paginated customers
        /// </summary>
        [HttpGet("paginated")]
        [ProducesResponseType(typeof(PaginatedResponse<Customer>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<Customer>>> GetPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _customerService.GetCustomersPaginatedAsync(pageNumber, pageSize, includeInactive);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaginated");
                return StatusCode(500, new PaginatedResponse<Customer>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get customer by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<Customer>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Customer>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<Customer>>> GetById(int id)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(id);
                if (customer == null)
                    return NotFound(ApiResponse<Customer>.ErrorResponse($"Customer with ID {id} not found"));

                return Ok(ApiResponse<Customer>.SuccessResponse(customer));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetById for ID {id}");
                return StatusCode(500, ApiResponse<Customer>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Create a new customer
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Customer>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<Customer>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<Customer>>> Create([FromBody] CustomerDto customerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<Customer>.ErrorResponse("Validation failed", errors));
                }

                var customer = await _customerService.CreateCustomerAsync(customerDto);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = customer.CustomerId },
                    ApiResponse<Customer>.SuccessResponse(customer, "Customer created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return StatusCode(500, ApiResponse<Customer>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Update an existing customer
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] CustomerUpdateDto customerDto)
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

                var success = await _customerService.UpdateCustomerAsync(id, customerDto);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Customer with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Customer updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Update for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Delete (soft delete) a customer
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            try
            {
                var success = await _customerService.DeleteCustomerAsync(id);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Customer with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Customer deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Delete for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Restore a deleted customer
        /// </summary>
        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Restore(int id)
        {
            try
            {
                var success = await _customerService.RestoreCustomerAsync(id);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Customer with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Customer restored successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Restore for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Search customers by name, phone, or address
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<Customer>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Customer>>>> Search(
            [FromQuery] string term,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                    return BadRequest(ApiResponse<List<Customer>>.ErrorResponse("Search term cannot be empty"));

                var customers = await _customerService.SearchCustomersAsync(term, includeInactive);
                return Ok(ApiResponse<List<Customer>>.SuccessResponse(customers, $"Found {customers.Count} customers"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Search with term '{term}'");
                return StatusCode(500, ApiResponse<List<Customer>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get customer balance
        /// </summary>
        [HttpGet("{id}/balance")]
        [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<decimal>>> GetBalance(int id)
        {
            try
            {
                var balance = await _customerService.GetCustomerBalanceAsync(id);
                return Ok(ApiResponse<decimal>.SuccessResponse(balance, "Balance retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetBalance for ID {id}");
                return StatusCode(500, ApiResponse<decimal>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Update customer balance
        /// </summary>
        [HttpPatch("{id}/balance")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateBalance(int id, [FromBody] decimal amount)
        {
            try
            {
                var success = await _customerService.UpdateCustomerBalanceAsync(id, amount);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Customer with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Balance updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in UpdateBalance for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }
    }
}