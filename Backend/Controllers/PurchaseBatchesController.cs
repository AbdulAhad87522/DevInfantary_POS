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

        /// <summary>
        /// Get all purchase batches
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<PurchaseBatch>>), StatusCodes.Status200OK)]
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
                return StatusCode(500, ApiResponse<List<PurchaseBatch>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get paginated batches with filters
        /// </summary>
        [HttpGet("paginated")]
        [ProducesResponseType(typeof(PaginatedResponse<PurchaseBatch>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<PurchaseBatch>>> GetPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? supplierId = null,
            [FromQuery] string? batchName = null,
            [FromQuery] string? status = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var filters = new PurchaseBatchSearchDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    SupplierId = supplierId,
                    BatchName = batchName,
                    Status = status
                };

                var result = await _batchService.GetBatchesPaginatedAsync(pageNumber, pageSize, filters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaginated");
                return StatusCode(500, new PaginatedResponse<PurchaseBatch>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get batch by ID (with items)
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PurchaseBatch>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PurchaseBatch>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<PurchaseBatch>>> GetById(int id)
        {
            try
            {
                var batch = await _batchService.GetBatchByIdAsync(id);
                if (batch == null)
                    return NotFound(ApiResponse<PurchaseBatch>.ErrorResponse($"Batch with ID {id} not found"));

                return Ok(ApiResponse<PurchaseBatch>.SuccessResponse(batch));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetById for ID {id}");
                return StatusCode(500, ApiResponse<PurchaseBatch>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get product variants for batch selection
        /// </summary>
        [HttpGet("variants")]
        [ProducesResponseType(typeof(ApiResponse<List<ProductVariantForBatch>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<ProductVariantForBatch>>>> GetVariants([FromQuery] string? search = null)
        {
            try
            {
                var variants = await _batchService.GetProductVariantsForBatchAsync(search);
                return Ok(ApiResponse<List<ProductVariantForBatch>>.SuccessResponse(variants,
                    $"Retrieved {variants.Count} product variants"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetVariants");
                return StatusCode(500, ApiResponse<List<ProductVariantForBatch>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get next batch ID
        /// </summary>
        [HttpGet("next-id")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<int>>> GetNextBatchId()
        {
            try
            {
                var nextId = await _batchService.GetNextBatchIdAsync();
                return Ok(ApiResponse<int>.SuccessResponse(nextId, "Next batch ID retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNextBatchId");
                return StatusCode(500, ApiResponse<int>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Create a new purchase batch
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PurchaseBatch>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<PurchaseBatch>), StatusCodes.Status400BadRequest)]
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
                    ApiResponse<PurchaseBatch>.SuccessResponse(batch,
                        "Purchase batch created successfully. Stock has been updated."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return StatusCode(500, ApiResponse<PurchaseBatch>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Update an existing purchase batch (header only)
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
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
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Batch with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Batch updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Update for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Delete a purchase batch (reverses stock)
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            try
            {
                var success = await _batchService.DeleteBatchAsync(id);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Batch with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Batch deleted successfully. Stock has been reversed."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Delete for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Search batches
        /// </summary>
        [HttpPost("search")]
        [ProducesResponseType(typeof(ApiResponse<List<PurchaseBatch>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<PurchaseBatch>>>> Search([FromBody] PurchaseBatchSearchDto searchDto)
        {
            try
            {
                var batches = await _batchService.SearchBatchesAsync(searchDto);
                return Ok(ApiResponse<List<PurchaseBatch>>.SuccessResponse(batches, $"Found {batches.Count} batches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Search");
                return StatusCode(500, ApiResponse<List<PurchaseBatch>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get batches by supplier
        /// </summary>
        [HttpGet("supplier/{supplierId}")]
        [ProducesResponseType(typeof(ApiResponse<List<PurchaseBatch>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<PurchaseBatch>>>> GetBySupplier(int supplierId)
        {
            try
            {
                var batches = await _batchService.GetBatchesBySupplierAsync(supplierId);
                return Ok(ApiResponse<List<PurchaseBatch>>.SuccessResponse(batches, $"Found {batches.Count} batches for supplier"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetBySupplier for supplier {supplierId}");
                return StatusCode(500, ApiResponse<List<PurchaseBatch>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get pending batches
        /// </summary>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(ApiResponse<List<PurchaseBatch>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<PurchaseBatch>>>> GetPending()
        {
            try
            {
                var batches = await _batchService.GetPendingBatchesAsync();
                return Ok(ApiResponse<List<PurchaseBatch>>.SuccessResponse(batches, $"Found {batches.Count} pending batches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPending");
                return StatusCode(500, ApiResponse<List<PurchaseBatch>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }
    }
}