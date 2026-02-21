using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HardwareStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ReturnsController : ControllerBase
    {
        private readonly IReturnService _returnService;
        private readonly ILogger<ReturnsController> _logger;

        public ReturnsController(IReturnService returnService, ILogger<ReturnsController> logger)
        {
            _returnService = returnService;
            _logger = logger;
        }

        /// <summary>
        /// Get all returns
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<Return>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Return>>>> GetAll()
        {
            try
            {
                var returns = await _returnService.GetAllReturnsAsync();
                return Ok(ApiResponse<List<Return>>.SuccessResponse(returns, $"Retrieved {returns.Count} returns"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll");
                return StatusCode(500, ApiResponse<List<Return>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get paginated returns with filters
        /// </summary>
        [HttpGet("paginated")]
        [ProducesResponseType(typeof(PaginatedResponse<Return>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<Return>>> GetPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? customerId = null,
            [FromQuery] int? billId = null,
            [FromQuery] string? status = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var filters = new ReturnSearchDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    CustomerId = customerId,
                    BillId = billId,
                    Status = status
                };

                var result = await _returnService.GetReturnsPaginatedAsync(pageNumber, pageSize, filters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaginated");
                return StatusCode(500, new PaginatedResponse<Return>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get return by ID (with items)
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<Return>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Return>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<Return>>> GetById(int id)
        {
            try
            {
                var returnRecord = await _returnService.GetReturnByIdAsync(id);
                if (returnRecord == null)
                    return NotFound(ApiResponse<Return>.ErrorResponse($"Return with ID {id} not found"));

                return Ok(ApiResponse<Return>.SuccessResponse(returnRecord));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetById for ID {id}");
                return StatusCode(500, ApiResponse<Return>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get bill details for processing return (bill + items)
        /// </summary>
        [HttpGet("bill/{billNumber}")]
        [ProducesResponseType(typeof(ApiResponse<BillForReturnDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BillForReturnDto>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<BillForReturnDto>>> GetBillForReturn(string billNumber)
        {
            try
            {
                var bill = await _returnService.GetBillForReturnAsync(billNumber);
                if (bill == null)
                    return NotFound(ApiResponse<BillForReturnDto>.ErrorResponse($"Bill '{billNumber}' not found"));

                return Ok(ApiResponse<BillForReturnDto>.SuccessResponse(bill,
                    $"Bill loaded with {bill.Items.Count} items"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetBillForReturn for {billNumber}");
                return StatusCode(500, ApiResponse<BillForReturnDto>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Process a customer return
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Return>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<Return>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<Return>>> ProcessReturn([FromBody] ProcessReturnDto returnDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<Return>.ErrorResponse("Validation failed", errors));
                }

                var returnRecord = await _returnService.ProcessReturnAsync(returnDto);

                string stockMessage = returnDto.RestoreStock
                    ? "Stock has been restored to inventory."
                    : "Stock was not restored.";

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = returnRecord.ReturnId },
                    ApiResponse<Return>.SuccessResponse(returnRecord,
                        $"Return processed successfully. {stockMessage}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessReturn");
                return StatusCode(500, ApiResponse<Return>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Search returns with filters
        /// </summary>
        [HttpPost("search")]
        [ProducesResponseType(typeof(ApiResponse<List<Return>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Return>>>> Search([FromBody] ReturnSearchDto searchDto)
        {
            try
            {
                var returns = await _returnService.SearchReturnsAsync(searchDto);
                return Ok(ApiResponse<List<Return>>.SuccessResponse(returns, $"Found {returns.Count} returns"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Search");
                return StatusCode(500, ApiResponse<List<Return>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get returns by customer
        /// </summary>
        [HttpGet("customer/{customerId}")]
        [ProducesResponseType(typeof(ApiResponse<List<Return>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Return>>>> GetByCustomer(int customerId)
        {
            try
            {
                var returns = await _returnService.GetReturnsByCustomerAsync(customerId);
                return Ok(ApiResponse<List<Return>>.SuccessResponse(returns, $"Found {returns.Count} returns for customer"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetByCustomer for customer {customerId}");
                return StatusCode(500, ApiResponse<List<Return>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get returns by bill
        /// </summary>
        [HttpGet("bill-id/{billId}")]
        [ProducesResponseType(typeof(ApiResponse<List<Return>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Return>>>> GetByBill(int billId)
        {
            try
            {
                var returns = await _returnService.GetReturnsByBillAsync(billId);
                return Ok(ApiResponse<List<Return>>.SuccessResponse(returns, $"Found {returns.Count} returns for bill"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetByBill for bill {billId}");
                return StatusCode(500, ApiResponse<List<Return>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }
    }
}