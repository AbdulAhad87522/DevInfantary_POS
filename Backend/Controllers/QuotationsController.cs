using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HardwareStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuotationsController : ControllerBase
    {
        private readonly IQuotationService _quotationService;
        private readonly IProductService _productService;
        private readonly ILogger<QuotationsController> _logger;

        public QuotationsController(IQuotationService quotationService, ILogger<QuotationsController> logger)
        {
            _quotationService = quotationService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new quotation (no PDF, no stock changes)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Quotation>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<Quotation>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<Quotation>>> Create([FromBody] CreateQuotationDto quotationDto)
        {
            try
            {
                var quotation = await _quotationService.CreateQuotationAsync(quotationDto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = quotation.QuotationId },
                    ApiResponse<Quotation>.SuccessResponse(
                        quotation,
                        $"Quotation {quotation.QuotationNumber} created successfully"
                    ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quotation");
                return BadRequest(ApiResponse<Quotation>.ErrorResponse(
                    "Failed to create quotation",
                    new List<string> { ex.Message }
                ));
            }
        }

        /// <summary>
        /// Generate and download PDF for a quotation
        /// </summary>
        [HttpGet("{id}/pdf")]
        [ProducesResponseType(typeof(ApiResponse<QuotationPdfResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<QuotationPdfResponse>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<QuotationPdfResponse>>> GeneratePdf(int id)
        {
            try
            {
                var pdfResponse = await _quotationService.GenerateQuotationPdfAsync(id);

                return Ok(ApiResponse<QuotationPdfResponse>.SuccessResponse(
                    pdfResponse,
                    $"PDF generated for quotation {pdfResponse.QuotationNumber}"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating PDF for quotation {id}");
                return NotFound(ApiResponse<QuotationPdfResponse>.ErrorResponse(
                    "Failed to generate PDF",
                    new List<string> { ex.Message }
                ));
            }
        }

        /// <summary>
        /// Download quotation PDF by quotation number
        /// </summary>
        [HttpGet("number/{quotationNumber}/pdf")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadQuotationPdfByNumber(string quotationNumber)
        {
            try
            {
                string pdfDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "quotations");

                if (!Directory.Exists(pdfDirectory))
                    return NotFound(new { message = "Quotations directory not found" });

                var files = Directory.GetFiles(pdfDirectory, $"{quotationNumber}_*.pdf");

                if (files.Length == 0)
                    return NotFound(new { message = $"PDF not found for quotation {quotationNumber}" });

                var pdfBytes = await System.IO.File.ReadAllBytesAsync(files[0]);
                var fileName = Path.GetFileName(files[0]);

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading PDF for quotation {quotationNumber}");
                return StatusCode(500, new { message = "Error downloading PDF" });
            }
        }

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
                _logger.LogError(ex, $"Error retrieving quotation {id}");
                return StatusCode(500, ApiResponse<Quotation>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

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
                _logger.LogError(ex, "Error retrieving quotations");
                return StatusCode(500, ApiResponse<List<Quotation>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get quotation by quotation number (with items)
        /// </summary>
        [HttpGet("search/{quotationNumber}")]
        [ProducesResponseType(typeof(ApiResponse<Quotation>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Quotation>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<Quotation>>> GetByQuotationNumber(string quotationNumber)
        {
            try
            {
                var quotation = await _quotationService.GetQuotationByNumberAsync(quotationNumber);

                if (quotation == null)
                {
                    return NotFound(ApiResponse<Quotation>.ErrorResponse(
                        $"Quotation with number '{quotationNumber}' not found"
                    ));
                }

                return Ok(ApiResponse<Quotation>.SuccessResponse(
                    quotation,
                    $"Found quotation {quotation.QuotationNumber} with {quotation.Items.Count} items"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving quotation by number {quotationNumber}");
                return StatusCode(500, ApiResponse<Quotation>.ErrorResponse(
                    "Internal server error",
                    new List<string> { ex.Message }
                ));
            }
        }

        [HttpPost("search")]
        [ProducesResponseType(typeof(ApiResponse<List<Quotation>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Quotation>>>> Search([FromBody] QuotationSearchDto searchDto)
        {
            try
            {
                var quotations = await _quotationService.SearchQuotationsAsync(searchDto);
                return Ok(ApiResponse<List<Quotation>>.SuccessResponse(quotations, $"Found {quotations.Count} quotations"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching quotations");
                return StatusCode(500, ApiResponse<List<Quotation>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }
    }
}