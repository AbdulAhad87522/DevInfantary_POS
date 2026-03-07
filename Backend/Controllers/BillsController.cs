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
        /// Create a new bill/sale and generate PDF
        /// </summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<BillWithPdfResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<BillWithPdfResponse>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<BillWithPdfResponse>>> Create([FromBody] CreateBillDto billDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<BillWithPdfResponse>.ErrorResponse("Validation failed", errors));
                }

                // Get staffId from JWT token if available (optional)
                int staffId = 1; // Default
                if (User.Identity?.IsAuthenticated == true)
                {
                    var staffIdClaim = User.FindFirst("StaffId")?.Value;
                    if (!string.IsNullOrEmpty(staffIdClaim))
                    {
                        staffId = int.Parse(staffIdClaim);
                    }
                }

                var result = await _billService.CreateBillAsync(billDto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Bill.BillId },
                    ApiResponse<BillWithPdfResponse>.SuccessResponse(
                        result,
                        $"Bill {result.Bill.BillNumber} created successfully. PDF generated."
                    )
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return StatusCode(500, ApiResponse<BillWithPdfResponse>.ErrorResponse(
                    "Internal server error",
                    new List<string> { ex.Message }
                ));
            }
        }

        /// <summary>
        /// Download bill PDF by bill ID
        /// </summary>
        [HttpGet("{id}/pdf")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadBillPdf(int id)
        {
            try
            {
                var bill = await _billService.GetBillByIdAsync(id);
                if (bill == null)
                    return NotFound(new { message = $"Bill with ID {id} not found" });

                // ✅ Define bills directory path
                string pdfDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "bills");

                // ✅ Create directory if it doesn't exist
                if (!Directory.Exists(pdfDirectory))
                {
                    Directory.CreateDirectory(pdfDirectory);
                    return NotFound(new { message = $"No PDF found for bill {bill.BillNumber}. The bill may have been created before PDF generation was enabled." });
                }

                // ✅ Search for PDF file
                var files = Directory.GetFiles(pdfDirectory, $"{bill.BillNumber}_*.pdf");

                if (files.Length == 0)
                    return NotFound(new { message = $"PDF not found for bill {bill.BillNumber}" });

                var pdfBytes = await System.IO.File.ReadAllBytesAsync(files[0]);
                var fileName = Path.GetFileName(files[0]);

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading PDF for bill {id}");
                return StatusCode(500, new { message = "Error downloading PDF", error = ex.Message });
            }
        }
        /// <summary>
        /// Download bill PDF by bill number
        /// </summary>
        [HttpGet("number/{billNumber}/pdf")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadBillPdfByNumber(string billNumber)
        {
            try
            {
                string pdfDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "bills");

                if (!Directory.Exists(pdfDirectory))
                    return NotFound(new { message = "Bills directory not found" });

                var files = Directory.GetFiles(pdfDirectory, $"{billNumber}_*.pdf");

                if (files.Length == 0)
                    return NotFound(new { message = $"PDF not found for bill {billNumber}" });

                var pdfBytes = await System.IO.File.ReadAllBytesAsync(files[0]);
                var fileName = Path.GetFileName(files[0]);

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading PDF for bill number {billNumber}");
                return StatusCode(500, new { message = "Error downloading PDF" });
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