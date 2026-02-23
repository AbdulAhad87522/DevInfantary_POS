using HardwareStoreAPI.Models;

namespace HardwareStoreAPI.Services
{
    public interface IDashboardService
    {
        Task<DashboardStats> GetDashboardStatsAsync();
        Task<List<TopProduct>> GetTopProductsAsync(int limit = 10);
        Task<List<RecentBill>> GetRecentBillsAsync(int limit = 10);
        Task<List<LowStockItem>> GetLowStockItemsAsync(int limit = 10);
        Task<List<SalesChartData>> GetSalesChartDataAsync(int days = 30);
        Task<List<CategorySales>> GetCategorySalesAsync();
        Task<List<PaymentMethodStats>> GetPaymentMethodStatsAsync();
    }
}