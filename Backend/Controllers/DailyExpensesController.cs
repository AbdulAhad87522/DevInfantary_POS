// Controllers/DailyExpensesController.cs
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
    public class DailyExpensesController : ControllerBase
    {
        private readonly IDailyExpenseService _expenseService;
        private readonly ILogger<DailyExpensesController> _logger;

        public DailyExpensesController(IDailyExpenseService expenseService, ILogger<DailyExpensesController> logger)
        {
            _expenseService = expenseService;
            _logger = logger;
        }

        /// <summary>Get all expenses</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<DailyExpense>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<DailyExpense>>>> GetAll()
        {
            try
            {
                var expenses = await _expenseService.GetAllExpensesAsync();
                return Ok(ApiResponse<List<DailyExpense>>.SuccessResponse(expenses, $"Retrieved {expenses.Count} expenses"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll");
                return StatusCode(500, ApiResponse<List<DailyExpense>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>Get paginated expenses</summary>
        [HttpGet("paginated")]
        [ProducesResponseType(typeof(PaginatedResponse<DailyExpense>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResponse<DailyExpense>>> GetPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _expenseService.GetExpensesPaginatedAsync(pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaginated");
                return StatusCode(500, new PaginatedResponse<DailyExpense> { Success = false, Message = "Internal server error" });
            }
        }

        /// <summary>Get expense by ID</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<DailyExpense>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<DailyExpense>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<DailyExpense>>> GetById(int id)
        {
            try
            {
                var expense = await _expenseService.GetExpenseByIdAsync(id);
                if (expense == null)
                    return NotFound(ApiResponse<DailyExpense>.ErrorResponse($"Expense with ID {id} not found"));

                return Ok(ApiResponse<DailyExpense>.SuccessResponse(expense));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetById for ID {id}");
                return StatusCode(500, ApiResponse<DailyExpense>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>Get expenses by date range</summary>
        [HttpGet("date-range")]
        [ProducesResponseType(typeof(ApiResponse<List<DailyExpense>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<DailyExpense>>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<List<DailyExpense>>>> GetByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                    return BadRequest(ApiResponse<List<DailyExpense>>.ErrorResponse("Start date cannot be after end date"));

                var expenses = await _expenseService.GetExpensesByDateRangeAsync(startDate, endDate);
                return Ok(ApiResponse<List<DailyExpense>>.SuccessResponse(expenses, $"Found {expenses.Count} expenses"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetByDateRange");
                return StatusCode(500, ApiResponse<List<DailyExpense>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>Search expenses by description</summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<DailyExpense>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<DailyExpense>>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<List<DailyExpense>>>> Search([FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                    return BadRequest(ApiResponse<List<DailyExpense>>.ErrorResponse("Search term cannot be empty"));

                var expenses = await _expenseService.SearchExpensesAsync(term);
                return Ok(ApiResponse<List<DailyExpense>>.SuccessResponse(expenses, $"Found {expenses.Count} expenses"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Search with term '{term}'");
                return StatusCode(500, ApiResponse<List<DailyExpense>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>Get total expense amount (optionally filtered by date range)</summary>
        [HttpGet("total")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<int>>> GetTotal(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                    return BadRequest(ApiResponse<int>.ErrorResponse("Start date cannot be after end date"));

                var total = await _expenseService.GetTotalAmountAsync(startDate, endDate);
                return Ok(ApiResponse<int>.SuccessResponse(total, $"Total expense amount: {total}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTotal");
                return StatusCode(500, ApiResponse<int>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>Create a new expense</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ProducesResponseType(typeof(ApiResponse<DailyExpense>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<DailyExpense>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<DailyExpense>>> Create([FromBody] DailyExpenseDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<DailyExpense>.ErrorResponse("Validation failed", errors));
                }

                var expense = await _expenseService.CreateExpenseAsync(dto);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = expense.ExpenseId },
                    ApiResponse<DailyExpense>.SuccessResponse(expense, "Expense created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return StatusCode(500, ApiResponse<DailyExpense>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>Update an existing expense</summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] DailyExpenseUpdateDto dto)
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

                var success = await _expenseService.UpdateExpenseAsync(id, dto);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Expense with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Expense updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Update for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>Delete an expense</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            try
            {
                var success = await _expenseService.DeleteExpenseAsync(id);
                if (!success)
                    return NotFound(ApiResponse<bool>.ErrorResponse($"Expense with ID {id} not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Expense deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Delete for ID {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }
    }
}