// Services/IDailyExpenseService.cs
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface IDailyExpenseService
    {
        Task<List<DailyExpense>> GetAllExpensesAsync();
        Task<PaginatedResponse<DailyExpense>> GetExpensesPaginatedAsync(int pageNumber, int pageSize);
        Task<DailyExpense?> GetExpenseByIdAsync(int id);
        Task<List<DailyExpense>> GetExpensesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<DailyExpense>> SearchExpensesAsync(string term);
        Task<int> GetTotalAmountAsync(DateTime? startDate, DateTime? endDate);
        Task<DailyExpense> CreateExpenseAsync(DailyExpenseDto dto);
        Task<bool> UpdateExpenseAsync(int id, DailyExpenseUpdateDto dto);
        Task<bool> DeleteExpenseAsync(int id);
    }
}