using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using HardwareStoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HardwareStoreAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Get complete dashboard statistics
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ApiResponse<DashboardStats>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<DashboardStats>>> GetStats()
        {
            try
            {
                var stats = await _dashboardService.GetDashboardStatsAsync();
                return Ok(ApiResponse<DashboardStats>.SuccessResponse(stats, "Dashboard statistics retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetStats");
                return StatusCode(500, ApiResponse<DashboardStats>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get top selling products
        /// </summary>
        [HttpGet("top-products")]
        [ProducesResponseType(typeof(ApiResponse<List<TopProduct>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<TopProduct>>>> GetTopProducts([FromQuery] int limit = 10)
        {
            try
            {
                var products = await _dashboardService.GetTopProductsAsync(limit);
                return Ok(ApiResponse<List<TopProduct>>.SuccessResponse(products, $"Retrieved top {products.Count} products"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTopProducts");
                return StatusCode(500, ApiResponse<List<TopProduct>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get recent bills
        /// </summary>
        [HttpGet("recent-bills")]
        [ProducesResponseType(typeof(ApiResponse<List<RecentBill>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<RecentBill>>>> GetRecentBills([FromQuery] int limit = 10)
        {
            try
            {
                var bills = await _dashboardService.GetRecentBillsAsync(limit);
                return Ok(ApiResponse<List<RecentBill>>.SuccessResponse(bills, $"Retrieved {bills.Count} recent bills"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecentBills");
                return StatusCode(500, ApiResponse<List<RecentBill>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get low stock items
        /// </summary>
        [HttpGet("low-stock")]
        [ProducesResponseType(typeof(ApiResponse<List<LowStockItem>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<LowStockItem>>>> GetLowStock([FromQuery] int limit = 10)
        {
            try
            {
                var items = await _dashboardService.GetLowStockItemsAsync(limit);
                return Ok(ApiResponse<List<LowStockItem>>.SuccessResponse(items, $"Retrieved {items.Count} low stock items"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLowStock");
                return StatusCode(500, ApiResponse<List<LowStockItem>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get sales trend chart data
        /// </summary>
        [HttpGet("sales-trend")]
        [ProducesResponseType(typeof(ApiResponse<List<SalesChartData>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<SalesChartData>>>> GetSalesTrend([FromQuery] int days = 30)
        {
            try
            {
                var data = await _dashboardService.GetSalesChartDataAsync(days);
                return Ok(ApiResponse<List<SalesChartData>>.SuccessResponse(data, $"Retrieved sales data for last {days} days"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSalesTrend");
                return StatusCode(500, ApiResponse<List<SalesChartData>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get sales by category
        /// </summary>
        [HttpGet("category-sales")]
        [ProducesResponseType(typeof(ApiResponse<List<CategorySales>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<CategorySales>>>> GetCategorySales()
        {
            try
            {
                var categories = await _dashboardService.GetCategorySalesAsync();
                return Ok(ApiResponse<List<CategorySales>>.SuccessResponse(categories, $"Retrieved sales for {categories.Count} categories"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategorySales");
                return StatusCode(500, ApiResponse<List<CategorySales>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get payment method statistics
        /// </summary>
        [HttpGet("payment-stats")]
        [ProducesResponseType(typeof(ApiResponse<List<PaymentMethodStats>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<PaymentMethodStats>>>> GetPaymentStats()
        {
            try
            {
                var stats = await _dashboardService.GetPaymentMethodStatsAsync();
                return Ok(ApiResponse<List<PaymentMethodStats>>.SuccessResponse(stats, "Payment statistics retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPaymentStats");
                return StatusCode(500, ApiResponse<List<PaymentMethodStats>>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get all dashboard data in one call
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(ApiResponse<DashboardData>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<DashboardData>>> GetAllDashboardData()
        {
            try
            {
                var dashboardData = new DashboardData
                {
                    Stats = await _dashboardService.GetDashboardStatsAsync(),
                    TopProducts = await _dashboardService.GetTopProductsAsync(10),
                    RecentBills = await _dashboardService.GetRecentBillsAsync(10),
                    LowStockItems = await _dashboardService.GetLowStockItemsAsync(10),
                    SalesChartData = await _dashboardService.GetSalesChartDataAsync(30),
                    CategorySales = await _dashboardService.GetCategorySalesAsync(),
                    PaymentMethodStats = await _dashboardService.GetPaymentMethodStatsAsync()
                };

                return Ok(ApiResponse<DashboardData>.SuccessResponse(dashboardData, "Complete dashboard data retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllDashboardData");
                return StatusCode(500, ApiResponse<DashboardData>.ErrorResponse("Internal server error", new List<string> { ex.Message }));
            }
        }
    }
}