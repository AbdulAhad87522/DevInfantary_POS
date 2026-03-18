using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HardwareStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockRequirementController : ControllerBase
    {
        private readonly IStockRequirementService _stockRequirementService;
        private readonly ILogger<StockRequirementController> _logger;

        public StockRequirementController(
            IStockRequirementService stockRequirementService,
            ILogger<StockRequirementController> logger)
        {
            _stockRequirementService = stockRequirementService;
            _logger = logger;
        }

        /// <summary>
        /// Generate stock requirement/reorder list
        /// Shows all items with stock at or below reorder level with prefilled quantities
        /// Optionally filter by supplier
        /// </summary>
        [HttpGet("generate")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<StockRequirementReportDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<StockRequirementReportDto>>> GenerateRequirementList(
            [FromQuery] int? supplierId = null)
        {
            try
            {
                var filters = new GenerateRequirementDto
                {
                    SupplierId = supplierId
                };

                var report = await _stockRequirementService.GenerateRequirementListAsync(filters);

                if (report.TotalItemsLowStock == 0)
                {
                    return Ok(ApiResponse<StockRequirementReportDto>.SuccessResponse(
                        report,
                        "No items require reordering at this time"
                    ));
                }

                string message = supplierId.HasValue
                    ? $"Generated requirement list for supplier: {report.TotalItemsLowStock} items need reordering. Estimated cost: Rs. {report.TotalEstimatedCost:N2}"
                    : $"Generated requirement list: {report.TotalItemsLowStock} items need reordering. Estimated cost: Rs. {report.TotalEstimatedCost:N2}";

                return Ok(ApiResponse<StockRequirementReportDto>.SuccessResponse(report, message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating stock requirement list");
                return StatusCode(500, ApiResponse<StockRequirementReportDto>.ErrorResponse(
                    "Internal server error",
                    new List<string> { ex.Message }
                ));
            }
        }
    }
}