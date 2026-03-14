// Controllers/CategoriesController.cs
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
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        /// <summary>
        /// Get all categories
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<Category>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Category>>>> GetAll([FromQuery] bool includeInactive = false)
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync(includeInactive);
                return Ok(ApiResponse<List<Category>>.SuccessResponse(categories, $"Retrieved {categories.Count} categories"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll");
                return StatusCode(500, ApiResponse<List<Category>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get paginated categories
        /// </summary>
        [HttpGet("paginated")]
        [ProducesResponseType(typeof(PaginatedResponse<Category>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<Category>>> GetPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _categoryService.GetCategoriesPaginatedAsync(pageNumber, pageSize, includeInactive);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaginated");
                return StatusCode(500, new PaginatedResponse<Category>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get category by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<Category>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<Category>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<Category>>> GetById(int id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null)
                    return NotFound(ApiResponse<Category>.ErrorResponse($"Category with ID {id} not found"));

                return Ok(ApiResponse<Category>.SuccessResponse(category));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetById for ID {id}");
                return StatusCode(500, ApiResponse<Category>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Create a new category
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Category>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<Category>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<Category>>> Create([FromBody] CategoryDto1 categoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<Category>.ErrorResponse("Validation failed", errors));
                }

                var category = await _categoryService.CreateCategoryAsync(categoryDto);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = category.LookupId },
                    ApiResponse<Category>.SuccessResponse(category, "Category created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return StatusCode(500, ApiResponse<Category>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Update an existing category
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] CategoryUpdateDto categoryDto)
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

                var success = await _categoryService.UpdateCategoryAsync(id, categoryDto);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Category with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Category updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Update for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Delete (soft delete) a category
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            try
            {
                var success = await _categoryService.DeleteCategoryAsync(id);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Category with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Category deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Delete for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Restore a deleted category
        /// </summary>
        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Restore(int id)
        {
            try
            {
                var success = await _categoryService.RestoreCategoryAsync(id);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Category with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Category restored successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Restore for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Search categories by value or description
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<Category>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Category>>>> Search(
            [FromQuery] string term,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                    return BadRequest(ApiResponse<List<Category>>.ErrorResponse("Search term cannot be empty"));

                var categories = await _categoryService.SearchCategoriesAsync(term, includeInactive);
                return Ok(ApiResponse<List<Category>>.SuccessResponse(categories, $"Found {categories.Count} categories"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Search with term '{term}'");
                return StatusCode(500, ApiResponse<List<Category>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }
    }
}