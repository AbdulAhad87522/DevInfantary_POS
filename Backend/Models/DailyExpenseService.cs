// Models/DailyExpense.cs
namespace HardwareStoreAPI.Models
{
    public class DailyExpense
    {
        public int ExpenseId { get; set; }
        public string? Description { get; set; }
        public DateTime Date { get; set; }
        public int Amount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}