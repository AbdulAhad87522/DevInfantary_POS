using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HardwareStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CustomerBillsController : ControllerBase
    {
        private readonly ICustomerBillService _billService;
        private readonly ILogger<CustomerBillsController> _logger;

        public CustomerBillsController(ICustomerBillService billService, ILogger<CustomerBillsController> logger)
        {
            _billService = billService;
            _logger = logger;
        }

        /// <summary>
        /// Get all customer bill summaries (grouped by customer)
        /// </summary>
        [HttpGet("summaries")]
        [ProducesResponseType(typeof(ApiResponse<List<CustomerBillSummary>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<CustomerBillSummary>>>> GetAllSummaries([FromQuery] string? search = null)
        {
            try
            {
                var summaries = await _billService.GetAllCustomerBillSummariesAsync(search);
                return Ok(ApiResponse<List<CustomerBillSummary>>.SuccessResponse(summaries,
                    $"Retrieved {summaries.Count} customer bill summaries"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllSummaries");
                return StatusCode(500, ApiResponse<List<CustomerBillSummary>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get bill summary for a specific customer
        /// </summary>
        [HttpGet("customer/{customerId}/summary")]
        [ProducesResponseType(typeof(ApiResponse<CustomerBillSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CustomerBillSummary>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<CustomerBillSummary>>> GetCustomerSummary(int customerId)
        {
            try
            {
                var summary = await _billService.GetCustomerBillSummaryAsync(customerId);
                if (summary == null)
                    return NotFound(ApiResponse<CustomerBillSummary>.ErrorResponse($"No bills found for customer {customerId}"));

                return Ok(ApiResponse<CustomerBillSummary>.SuccessResponse(summary));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetCustomerSummary for customer {customerId}");
                return StatusCode(500, ApiResponse<CustomerBillSummary>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get all bills for a specific customer
        /// </summary>
        [HttpGet("customer/{customerId}")]
        [ProducesResponseType(typeof(ApiResponse<List<CustomerBillDetail>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<CustomerBillDetail>>>> GetCustomerBills(int customerId)
        {
            try
            {
                var bills = await _billService.GetCustomerBillsAsync(customerId);
                return Ok(ApiResponse<List<CustomerBillDetail>>.SuccessResponse(bills,
                    $"Retrieved {bills.Count} bills for customer"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetCustomerBills for customer {customerId}");
                return StatusCode(500, ApiResponse<List<CustomerBillDetail>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get bill detail with items
        /// </summary>
        [HttpGet("bill/{billId}")]
        [ProducesResponseType(typeof(ApiResponse<CustomerBillDetail>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CustomerBillDetail>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<CustomerBillDetail>>> GetBillDetail(int billId)
        {
            try
            {
                var bill = await _billService.GetBillDetailAsync(billId);
                if (bill == null)
                    return NotFound(ApiResponse<CustomerBillDetail>.ErrorResponse($"Bill with ID {billId} not found"));

                return Ok(ApiResponse<CustomerBillDetail>.SuccessResponse(bill));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetBillDetail for bill {billId}");
                return StatusCode(500, ApiResponse<CustomerBillDetail>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get payment records for a customer
        /// </summary>
        [HttpGet("customer/{customerId}/payments")]
        [ProducesResponseType(typeof(ApiResponse<List<CustomerPaymentRecord>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<CustomerPaymentRecord>>>> GetPaymentRecords(int customerId)
        {
            try
            {
                var records = await _billService.GetCustomerPaymentRecordsAsync(customerId);
                return Ok(ApiResponse<List<CustomerPaymentRecord>>.SuccessResponse(records,
                    $"Retrieved {records.Count} payment records"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetPaymentRecords for customer {customerId}");
                return StatusCode(500, ApiResponse<List<CustomerPaymentRecord>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Add payment for customer (distributed across unpaid bills)
        /// </summary>
        [HttpPost("payment")]
        [ProducesResponseType(typeof(ApiResponse<PaymentDistributionResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PaymentDistributionResult>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<PaymentDistributionResult>>> AddPayment([FromBody] AddCustomerPaymentDto paymentDto)
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

                var result = await _billService.AddCustomerPaymentAsync(paymentDto);

                string message = result.Remaining > 0
                    ? $"Payment of Rs. {result.Applied:N2} applied. Remaining Rs. {result.Remaining:N2} credited to customer account."
                    : $"Payment of Rs. {result.Applied:N2} applied successfully across {result.Allocations.Count} bill(s).";

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