using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HardwareStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PurchaseBatchesController : ControllerBase
    {
        private readonly IPurchaseBatchService _batchService;
        private readonly ILogger<PurchaseBatchesController> _logger;

        public PurchaseBatchesController(IPurchaseBatchService batchService, ILogger<PurchaseBatchesController> logger)
        {
            _batchService = batchService;
            _logger = logger;
        }

        #region Batch Endpoints

        /// <summary>
        /// Get all purchase batches
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<PurchaseBatch>>>> GetAll()
        {
            try
            {
                var batches = await _batchService.GetAllBatchesAsync();
                return Ok(ApiResponse<List<PurchaseBatch>>.SuccessResponse(batches, $"Retrieved {batches.Count} batches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll");
                return StatusCode(500, ApiResponse<List<PurchaseBatch>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get paginated purchase batches
        /// </summary>
        [HttpGet("paginated")]
        public async Task<ActionResult<PaginatedResponse<PurchaseBatch>>> GetPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? supplierId = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var filters = new PurchaseBatchSearchDto
                {
                    SearchTerm = searchTerm,
                    SupplierId = supplierId,
                    Status = status,
                    StartDate = startDate,
                    EndDate = endDate,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var result = await _batchService.GetBatchesPaginatedAsync(pageNumber, pageSize, filters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaginated");
                return StatusCode(500, new PaginatedResponse<PurchaseBatch> { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get purchase batch by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PurchaseBatch>>> GetById(int id)
        {
            try
            {
                var batch = await _batchService.GetBatchByIdAsync(id);
                if (batch == null)
                    return NotFound(ApiResponse<PurchaseBatch>.ErrorResponse($"Purchase batch with ID {id} not found"));

                return Ok(ApiResponse<PurchaseBatch>.SuccessResponse(batch));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetById for ID {id}");
                return StatusCode(500, ApiResponse<PurchaseBatch>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Create a new purchase batch
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<PurchaseBatch>>> Create([FromBody] CreatePurchaseBatchDto batchDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<PurchaseBatch>.ErrorResponse("Validation failed", errors));
                }

                var batch = await _batchService.CreateBatchAsync(batchDto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = batch.BatchId },
                    ApiResponse<PurchaseBatch>.SuccessResponse(batch, "Purchase batch created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<PurchaseBatch>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return StatusCode(500, ApiResponse<PurchaseBatch>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Update an existing purchase batch
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] UpdatePurchaseBatchDto batchDto)
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

                var success = await _batchService.UpdateBatchAsync(id, batchDto);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Purchase batch with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Purchase batch updated successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Update for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Delete a purchase batch
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            try
            {
                var success = await _batchService.DeleteBatchAsync(id);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Purchase batch with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Purchase batch deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Delete for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        #endregion

        #region Batch Items Endpoints

        /// <summary>
        /// Get items for a specific batch
        /// </summary>
        [HttpGet("{batchId}/items")]
        public async Task<ActionResult<ApiResponse<List<PurchaseBatchItem>>>> GetBatchItems(int batchId)
        {
            try
            {
                var items = await _batchService.GetBatchItemsAsync(batchId);
                return Ok(ApiResponse<List<PurchaseBatchItem>>.SuccessResponse(items, $"Found {items.Count} items"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting items for batch {batchId}");
                return StatusCode(500, ApiResponse<List<PurchaseBatchItem>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get batch item by ID
        /// </summary>
        [HttpGet("items/{itemId}")]
        public async Task<ActionResult<ApiResponse<PurchaseBatchItem>>> GetBatchItemById(int itemId)
        {
            try
            {
                var item = await _batchService.GetBatchItemByIdAsync(itemId);
                if (item == null)
                    return NotFound(ApiResponse<PurchaseBatchItem>.ErrorResponse($"Batch item with ID {itemId} not found"));

                return Ok(ApiResponse<PurchaseBatchItem>.SuccessResponse(item));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting batch item {itemId}");
                return StatusCode(500, ApiResponse<PurchaseBatchItem>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Add item to a batch
        /// </summary>
        [HttpPost("{batchId}/items")]
        public async Task<ActionResult<ApiResponse<PurchaseBatchItem>>> AddBatchItem(int batchId, [FromBody] CreatePurchaseBatchItemDto itemDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<PurchaseBatchItem>.ErrorResponse("Validation failed", errors));
                }

                var item = await _batchService.AddBatchItemAsync(batchId, itemDto);

                return CreatedAtAction(
                    nameof(GetBatchItemById),
                    new { itemId = item.PurchaseBatchItemId },
                    ApiResponse<PurchaseBatchItem>.SuccessResponse(item, "Item added to batch successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<PurchaseBatchItem>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding item to batch {batchId}");
                return StatusCode(500, ApiResponse<PurchaseBatchItem>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Update batch item
        /// </summary>
        [HttpPut("items/{itemId}")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateBatchItem(int itemId, [FromBody] UpdatePurchaseBatchItemDto itemDto)
        {
            try
            {
                if (itemId != itemDto.PurchaseBatchItemId)
                {
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Item ID mismatch"));
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Validation failed", errors));
                }

                var success = await _batchService.UpdateBatchItemAsync(itemId, itemDto);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Batch item with ID {itemId} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Batch item updated successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating batch item {itemId}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Delete batch item
        /// </summary>
        [HttpDelete("items/{itemId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteBatchItem(int itemId)
        {
            try
            {
                var success = await _batchService.DeleteBatchItemAsync(itemId);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Batch item with ID {itemId} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Batch item deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting batch item {itemId}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        #endregion

        #region Variant Selection (matching your UI)

        /// <summary>
        /// Get variants for selection (like your UI grid)
        /// </summary>
        [HttpGet("variants-for-selection")]
        public async Task<ActionResult<ApiResponse<List<VariantForSelectionDto>>>> GetVariantsForSelection([FromQuery] string? search = null)
        {
            try
            {
                var variants = await _batchService.GetVariantsForSelectionAsync(search);
                return Ok(ApiResponse<List<VariantForSelectionDto>>.SuccessResponse(variants, $"Found {variants.Count} variants"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting variants for selection");
                return StatusCode(500, ApiResponse<List<VariantForSelectionDto>>.ErrorResponse("Internal server error"));
            }
        }

        #endregion

        #region Search & Reports

        /// <summary>
        /// Advanced search for batches
        /// </summary>
        [HttpPost("search")]
        public async Task<ActionResult<ApiResponse<List<PurchaseBatch>>>> Search([FromBody] PurchaseBatchSearchDto searchDto)
        {
            try
            {
                var batches = await _batchService.SearchBatchesAsync(searchDto);
                return Ok(ApiResponse<List<PurchaseBatch>>.SuccessResponse(batches, $"Found {batches.Count} batches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching batches");
                return StatusCode(500, ApiResponse<List<PurchaseBatch>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get batch summaries (for dashboard)
        /// </summary>
        [HttpGet("summaries")]
        public async Task<ActionResult<ApiResponse<List<PurchaseBatchSummary>>>> GetSummaries()
        {
            try
            {
                var summaries = await _batchService.GetBatchSummariesAsync();
                return Ok(ApiResponse<List<PurchaseBatchSummary>>.SuccessResponse(summaries));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch summaries");
                return StatusCode(500, ApiResponse<List<PurchaseBatchSummary>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get batches by supplier
        /// </summary>
        [HttpGet("by-supplier/{supplierId}")]
        public async Task<ActionResult<ApiResponse<List<PurchaseBatch>>>> GetBySupplier(int supplierId)
        {
            try
            {
                var batches = await _batchService.GetBatchesBySupplierAsync(supplierId);
                return Ok(ApiResponse<List<PurchaseBatch>>.SuccessResponse(batches, $"Found {batches.Count} batches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting batches for supplier {supplierId}");
                return StatusCode(500, ApiResponse<List<PurchaseBatch>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get batches by status
        /// </summary>
        [HttpGet("by-status/{status}")]
        public async Task<ActionResult<ApiResponse<List<PurchaseBatch>>>> GetByStatus(string status)
        {
            try
            {
                var batches = await _batchService.GetBatchesByStatusAsync(status);
                return Ok(ApiResponse<List<PurchaseBatch>>.SuccessResponse(batches, $"Found {batches.Count} batches with status {status}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting batches with status {status}");
                return StatusCode(500, ApiResponse<List<PurchaseBatch>>.ErrorResponse("Internal server error"));
            }
        }

        #endregion

        #region Payment Management

        /// <summary>
        /// Make a payment on a batch
        /// </summary>
        [HttpPost("payment")]
        public async Task<ActionResult<ApiResponse<bool>>> MakePayment([FromBody] BatchPaymentDto paymentDto)
        {
            try
            {
                var success = await _batchService.MakePaymentAsync(paymentDto);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Batch with ID {paymentDto.BatchId} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Payment applied successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making payment");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get outstanding balance for a supplier
        /// </summary>
        [HttpGet("outstanding/{supplierId}")]
        public async Task<ActionResult<ApiResponse<decimal>>> GetOutstandingBalance(int supplierId)
        {
            try
            {
                var balance = await _batchService.GetOutstandingBalanceAsync(supplierId);
                return Ok(ApiResponse<decimal>.SuccessResponse(balance, $"Outstanding balance: {balance:C}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting outstanding balance for supplier {supplierId}");
                return StatusCode(500, ApiResponse<decimal>.ErrorResponse("Internal server error"));
            }
        }

        #endregion

        #region Stock Management

        /// <summary>
        /// Update stock from batch (if not automatically done)
        /// </summary>
        [HttpPost("{batchId}/update-stock")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateStockFromBatch(int batchId)
        {
            try
            {
                var success = await _batchService.UpdateStockFromBatchAsync(batchId);
                return Ok(ApiResponse<bool>.SuccessResponse(success, "Stock updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating stock from batch {batchId}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        #endregion
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                message = "Controller is working",
                time = DateTime.Now,
                controller = "PurchaseBatches"
            });
        }

        #region Utility

        /// <summary>
        /// Get next available batch ID
        /// </summary>
        [HttpGet("next-id")]
        public async Task<ActionResult<ApiResponse<int>>> GetNextBatchId()
        {
            try
            {
                var nextId = await _batchService.GetNextBatchIdAsync();
                return Ok(ApiResponse<int>.SuccessResponse(nextId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next batch ID");
                return StatusCode(500, ApiResponse<int>.ErrorResponse("Internal server error"));
            }
        }

        #endregion
    }

}