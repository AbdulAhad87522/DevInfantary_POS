using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HardwareStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SupplierBillsController : ControllerBase
    {
        private readonly ISupplierBillService _billService;
        private readonly ILogger<SupplierBillsController> _logger;

        public SupplierBillsController(ISupplierBillService billService, ILogger<SupplierBillsController> logger)
        {
            _billService = billService;
            _logger = logger;
        }

        /// <summary>
        /// Get all supplier bill summaries (grouped by supplier)
        /// </summary>
        [HttpGet("summaries")]
        [ProducesResponseType(typeof(ApiResponse<List<SupplierBillSummary>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<SupplierBillSummary>>>> GetAllSummaries([FromQuery] string? search = null)
        {
            try
            {
                var summaries = await _billService.GetAllSupplierBillSummariesAsync(search);
                return Ok(ApiResponse<List<SupplierBillSummary>>.SuccessResponse(summaries,
                    $"Retrieved {summaries.Count} supplier bill summaries"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllSummaries");
                return StatusCode(500, ApiResponse<List<SupplierBillSummary>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get bill summary for a specific supplier
        /// </summary>
        [HttpGet("supplier/{supplierId}/summary")]
        [ProducesResponseType(typeof(ApiResponse<SupplierBillSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SupplierBillSummary>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<SupplierBillSummary>>> GetSupplierSummary(int supplierId)
        {
            try
            {
                var summary = await _billService.GetSupplierBillSummaryAsync(supplierId);
                if (summary == null)
                    return NotFound(ApiResponse<SupplierBillSummary>.ErrorResponse($"No batches found for supplier {supplierId}"));

                return Ok(ApiResponse<SupplierBillSummary>.SuccessResponse(summary));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetSupplierSummary for supplier {supplierId}");
                return StatusCode(500, ApiResponse<SupplierBillSummary>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get all batches for a specific supplier
        /// </summary>
        [HttpGet("supplier/{supplierId}/batches")]
        [ProducesResponseType(typeof(ApiResponse<List<SupplierBatchDetail>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<SupplierBatchDetail>>>> GetSupplierBatches(int supplierId)
        {
            try
            {
                var batches = await _billService.GetSupplierBatchesAsync(supplierId);
                return Ok(ApiResponse<List<SupplierBatchDetail>>.SuccessResponse(batches,
                    $"Retrieved {batches.Count} batches for supplier"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetSupplierBatches for supplier {supplierId}");
                return StatusCode(500, ApiResponse<List<SupplierBatchDetail>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get batch detail with items
        /// </summary>
        [HttpGet("batch/{batchId}")]
        [ProducesResponseType(typeof(ApiResponse<SupplierBatchDetail>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SupplierBatchDetail>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<SupplierBatchDetail>>> GetBatchDetail(int batchId)
        {
            try
            {
                var batch = await _billService.GetBatchDetailAsync(batchId);
                if (batch == null)
                    return NotFound(ApiResponse<SupplierBatchDetail>.ErrorResponse($"Batch with ID {batchId} not found"));

                return Ok(ApiResponse<SupplierBatchDetail>.SuccessResponse(batch));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetBatchDetail for batch {batchId}");
                return StatusCode(500, ApiResponse<SupplierBatchDetail>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get payment records for a supplier
        /// </summary>
        [HttpGet("supplier/{supplierId}/payments")]
        [ProducesResponseType(typeof(ApiResponse<List<SupplierPaymentRecord>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<SupplierPaymentRecord>>>> GetSupplierPayments(int supplierId)
        {
            try
            {
                var records = await _billService.GetSupplierPaymentRecordsAsync(supplierId);
                return Ok(ApiResponse<List<SupplierPaymentRecord>>.SuccessResponse(records,
                    $"Retrieved {records.Count} payment records"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetSupplierPayments for supplier {supplierId}");
                return StatusCode(500, ApiResponse<List<SupplierPaymentRecord>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get payment records for a batch
        /// </summary>
        [HttpGet("batch/{batchId}/payments")]
        [ProducesResponseType(typeof(ApiResponse<List<SupplierPaymentRecord>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<SupplierPaymentRecord>>>> GetBatchPayments(int batchId)
        {
            try
            {
                var records = await _billService.GetBatchPaymentRecordsAsync(batchId);
                return Ok(ApiResponse<List<SupplierPaymentRecord>>.SuccessResponse(records,
                    $"Retrieved {records.Count} payment records"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetBatchPayments for batch {batchId}");
                return StatusCode(500, ApiResponse<List<SupplierPaymentRecord>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Update supplier batch/bill
        /// </summary>
        [HttpPut("batch/{batchId}")]
        [ProducesResponseType(typeof(ApiResponse<SupplierBatchDetail>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SupplierBatchDetail>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<SupplierBatchDetail>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<SupplierBatchDetail>>> UpdateBatch(
            int batchId,
            [FromBody] UpdateSupplierBatchDto updateDto)
        {
            try
            {
                if (batchId != updateDto.BatchId)
                    return BadRequest(ApiResponse<SupplierBatchDetail>.ErrorResponse("Batch ID mismatch"));

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<SupplierBatchDetail>.ErrorResponse("Validation failed", errors));
                }

                var updatedBatch = await _billService.UpdateSupplierBatchAsync(updateDto);

                return Ok(ApiResponse<SupplierBatchDetail>.SuccessResponse(
                    updatedBatch,
                    "Batch updated successfully"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating batch {batchId}");

                if (ex.Message.Contains("not found"))
                    return NotFound(ApiResponse<SupplierBatchDetail>.ErrorResponse(ex.Message));

                return StatusCode(500, ApiResponse<SupplierBatchDetail>.ErrorResponse(
                    "Internal server error",
                    new List<string> { ex.Message }
                ));
            }
        }

        /// <summary>
        /// Add payment for supplier (distributed across unpaid batches)
        /// </summary>
        [HttpPost("payment")]
        [ProducesResponseType(typeof(ApiResponse<PaymentDistributionResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PaymentDistributionResult>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<PaymentDistributionResult>>> AddPayment([FromBody] AddSupplierPaymentDto paymentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<PaymentDistributionResult>.ErrorResponse("Validation failed", errors));
                }

                var result = await _billService.AddSupplierPaymentAsync(paymentDto);

                string message = result.Remaining > 0
                    ? $"Payment of Rs. {result.Applied:N2} applied. Overpayment of Rs. {result.Remaining:N2} (no further batches to apply to)."
                    : $"Payment of Rs. {result.Applied:N2} applied successfully across {result.Allocations.Count} batch(es).";

                return Ok(ApiResponse<PaymentDistributionResult>.SuccessResponse(result, message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddPayment");
                return StatusCode(500, ApiResponse<PaymentDistributionResult>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }
    }
}