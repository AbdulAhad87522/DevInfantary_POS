using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HardwareStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BillsController : ControllerBase
    {
        private readonly IBillService _billService;
        private readonly ILogger<BillsController> _logger;

        public BillsController(IBillService billService, ILogger<BillsController> logger)
        {
            _billService = billService;
            _logger = logger;
        }

        /// <summary>
        /// Get all bills
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<Bill>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Bill>>>> GetAll()
        {
            try
            {
                var bills = await _billService.GetAllBillsAsync();
                return Ok(ApiResponse<List<Bill>>.SuccessResponse(bills, $"Retrieved {bills.Count} bills"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll");
                return StatusCode(500, ApiResponse<List<Bill>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get paginated bills with optional filters
        /// </summary>
        [HttpGet("paginated")]
        [ProducesResponseType(typeof(PaginatedResponse<Bill>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<Bill>>> GetPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? customerId = null,
            [FromQuery] string? billNumber = null,
            [FromQuery] string? paymentStatus = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var filters = new BillSearchDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    CustomerId = customerId,
                    BillNumber = billNumber,
                    PaymentStatus = paymentStatus
                };

                var result = await _billService.GetBillsPaginatedAsync(pageNumber, pageSize, filters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaginated");
                return StatusCode(500, new PaginatedResponse<Bill>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get bill by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<Bill>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Bill>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<Bill>>> GetById(int id)
        {
            try
            {
                var bill = await _billService.GetBillByIdAsync(id);
                if (bill == null)
                    return NotFound(ApiResponse<Bill>.ErrorResponse($"Bill with ID {id} not found"));

                return Ok(ApiResponse<Bill>.SuccessResponse(bill));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetById for ID {id}");
                return StatusCode(500, ApiResponse<Bill>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get bill by bill number
        /// </summary>
        [HttpGet("number/{billNumber}")]
        [ProducesResponseType(typeof(ApiResponse<Bill>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Bill>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<Bill>>> GetByNumber(string billNumber)
        {
            try
            {
                var bill = await _billService.GetBillByNumberAsync(billNumber);
                if (bill == null)
                    return NotFound(ApiResponse<Bill>.ErrorResponse($"Bill with number {billNumber} not found"));

                return Ok(ApiResponse<Bill>.SuccessResponse(bill));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetByNumber for {billNumber}");
                return StatusCode(500, ApiResponse<Bill>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Create a new bill/sale
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Bill>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<Bill>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<Bill>>> Create([FromBody] CreateBillDto billDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<Bill>.ErrorResponse("Validation failed", errors));
                }

                var bill = await _billService.CreateBillAsync(billDto, staffId: 1);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = bill.BillId },
                    ApiResponse<Bill>.SuccessResponse(bill, "Bill created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return StatusCode(500, ApiResponse<Bill>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Search bills
        /// </summary>
        [HttpPost("search")]
        [ProducesResponseType(typeof(ApiResponse<List<Bill>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Bill>>>> Search([FromBody] BillSearchDto searchDto)
        {
            try
            {
                var bills = await _billService.SearchBillsAsync(searchDto);
                return Ok(ApiResponse<List<Bill>>.SuccessResponse(bills, $"Found {bills.Count} bills"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Search");
                return StatusCode(500, ApiResponse<List<Bill>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get bills by customer
        /// </summary>
        [HttpGet("customer/{customerId}")]
        [ProducesResponseType(typeof(ApiResponse<List<Bill>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Bill>>>> GetByCustomer(int customerId)
        {
            try
            {
                var bills = await _billService.GetBillsByCustomerAsync(customerId);
                return Ok(ApiResponse<List<Bill>>.SuccessResponse(bills, $"Found {bills.Count} bills for customer"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetByCustomer for customer {customerId}");
                return StatusCode(500, ApiResponse<List<Bill>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get pending bills
        /// </summary>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(ApiResponse<List<Bill>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Bill>>>> GetPending()
        {
            try
            {
                var bills = await _billService.GetPendingBillsAsync();
                return Ok(ApiResponse<List<Bill>>.SuccessResponse(bills, $"Found {bills.Count} pending bills"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPending");
                return StatusCode(500, ApiResponse<List<Bill>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get customer outstanding balance
        /// </summary>
        [HttpGet("customer/{customerId}/outstanding")]
        [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<decimal>>> GetCustomerOutstanding(int customerId)
        {
            try
            {
                var balance = await _billService.GetCustomerOutstandingBalanceAsync(customerId);
                return Ok(ApiResponse<decimal>.SuccessResponse(balance, "Outstanding balance retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetCustomerOutstanding for customer {customerId}");
                return StatusCode(500, ApiResponse<decimal>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }
    }
}