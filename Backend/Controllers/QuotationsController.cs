using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HardwareStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class QuotationsController : ControllerBase
    {
        private readonly IQuotationService _quotationService;
        private readonly ILogger<QuotationsController> _logger;

        public QuotationsController(IQuotationService quotationService, ILogger<QuotationsController> logger)
        {
            _quotationService = quotationService;
            _logger = logger;
        }

        /// <summary>
        /// Get all quotations
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<Quotation>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Quotation>>>> GetAll()
        {
            try
            {
                var quotations = await _quotationService.GetAllQuotationsAsync();
                return Ok(ApiResponse<List<Quotation>>.SuccessResponse(quotations, $"Retrieved {quotations.Count} quotations"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll");
                return StatusCode(500, ApiResponse<List<Quotation>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get paginated quotations with optional filters
        /// </summary>
        [HttpGet("paginated")]
        [ProducesResponseType(typeof(PaginatedResponse<Quotation>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<Quotation>>> GetPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? customerId = null,
            [FromQuery] string? quotationNumber = null,
            [FromQuery] string? status = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var filters = new QuotationSearchDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    CustomerId = customerId,
                    QuotationNumber = quotationNumber,
                    Status = status
                };

                var result = await _quotationService.GetQuotationsPaginatedAsync(pageNumber, pageSize, filters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaginated");
                return StatusCode(500, new PaginatedResponse<Quotation>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get quotation by ID (with items)
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<Quotation>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Quotation>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<Quotation>>> GetById(int id)
        {
            try
            {
                var quotation = await _quotationService.GetQuotationByIdAsync(id);
                if (quotation == null)
                    return NotFound(ApiResponse<Quotation>.ErrorResponse($"Quotation with ID {id} not found"));

                return Ok(ApiResponse<Quotation>.SuccessResponse(quotation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetById for ID {id}");
                return StatusCode(500, ApiResponse<Quotation>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get quotation by quotation number (with items)
        /// </summary>
        [HttpGet("number/{quotationNumber}")]
        [ProducesResponseType(typeof(ApiResponse<Quotation>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Quotation>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<Quotation>>> GetByNumber(string quotationNumber)
        {
            try
            {
                var quotation = await _quotationService.GetQuotationByNumberAsync(quotationNumber);
                if (quotation == null)
                    return NotFound(ApiResponse<Quotation>.ErrorResponse($"Quotation with number {quotationNumber} not found"));

                return Ok(ApiResponse<Quotation>.SuccessResponse(quotation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetByNumber for {quotationNumber}");
                return StatusCode(500, ApiResponse<Quotation>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Search for a quotation by ID or number (with items) - MAIN SEARCH ENDPOINT
        /// </summary>
        [HttpGet("search/{searchValue}")]
        [ProducesResponseType(typeof(ApiResponse<Quotation>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Quotation>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<Quotation>>> Search(string searchValue)
        {
            try
            {
                var quotation = await _quotationService.SearchQuotationAsync(searchValue);
                if (quotation == null)
                    return NotFound(ApiResponse<Quotation>.ErrorResponse($"Quotation '{searchValue}' not found"));

                return Ok(ApiResponse<Quotation>.SuccessResponse(quotation,
                    $"Found quotation with {quotation.Items.Count} items"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Search for {searchValue}");
                return StatusCode(500, ApiResponse<Quotation>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Create a new quotation
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Quotation>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<Quotation>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<Quotation>>> Create([FromBody] CreateQuotationDto quotationDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<Quotation>.ErrorResponse("Validation failed", errors));
                }

                var quotation = await _quotationService.CreateQuotationAsync(quotationDto, staffId: 1);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = quotation.QuotationId },
                    ApiResponse<Quotation>.SuccessResponse(quotation, "Quotation created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return StatusCode(500, ApiResponse<Quotation>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Search quotations with filters
        /// </summary>
        [HttpPost("search")]
        [ProducesResponseType(typeof(ApiResponse<List<Quotation>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Quotation>>>> SearchQuotations([FromBody] QuotationSearchDto searchDto)
        {
            try
            {
                var quotations = await _quotationService.SearchQuotationsAsync(searchDto);
                return Ok(ApiResponse<List<Quotation>>.SuccessResponse(quotations, $"Found {quotations.Count} quotations"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchQuotations");
                return StatusCode(500, ApiResponse<List<Quotation>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get quotations by customer
        /// </summary>
        [HttpGet("customer/{customerId}")]
        [ProducesResponseType(typeof(ApiResponse<List<Quotation>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Quotation>>>> GetByCustomer(int customerId)
        {
            try
            {
                var quotations = await _quotationService.GetQuotationsByCustomerAsync(customerId);
                return Ok(ApiResponse<List<Quotation>>.SuccessResponse(quotations, $"Found {quotations.Count} quotations for customer"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetByCustomer for customer {customerId}");
                return StatusCode(500, ApiResponse<List<Quotation>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get pending quotations
        /// </summary>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(ApiResponse<List<Quotation>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Quotation>>>> GetPending()
        {
            try
            {
                var quotations = await _quotationService.GetPendingQuotationsAsync();
                return Ok(ApiResponse<List<Quotation>>.SuccessResponse(quotations, $"Found {quotations.Count} pending quotations"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPending");
                return StatusCode(500, ApiResponse<List<Quotation>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }
    }
}