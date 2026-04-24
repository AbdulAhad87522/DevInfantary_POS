// Services/DailyExpenseService.cs
using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using MySql.Data.MySqlClient;

namespace HardwareStoreAPI.Services
{
    public class DailyExpenseService : IDailyExpenseService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<DailyExpenseService> _logger;

        public DailyExpenseService(ILogger<DailyExpenseService> logger)
        {
            _db = DatabaseHelper.Instance;
            _logger = logger;
        }

        public async Task<List<DailyExpense>> GetAllExpensesAsync()
        {
            var expenseList = new List<DailyExpense>();

            string query = @"
                SELECT * FROM daily_expenses
                ORDER BY date DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    expenseList.Add(MapToExpense(reader));

                _logger.LogInformation($"Retrieved {expenseList.Count} expenses");
                return expenseList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all expenses");
                throw;
            }
        }

        public async Task<PaginatedResponse<DailyExpense>> GetExpensesPaginatedAsync(int pageNumber, int pageSize)
        {
            var response = new PaginatedResponse<DailyExpense>
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            try
            {
                string countQuery = "SELECT COUNT(*) FROM daily_expenses";
                response.TotalRecords = Convert.ToInt32(await _db.ExecuteScalarAsync(countQuery));

                int offset = (pageNumber - 1) * pageSize;
                string query = @"
                    SELECT * FROM daily_expenses
                    ORDER BY date DESC
                    LIMIT @pageSize OFFSET @offset";

                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@pageSize", pageSize);
                command.Parameters.AddWithValue("@offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    response.Data.Add(MapToExpense(reader));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated expenses");
                throw;
            }
        }

        public async Task<DailyExpense?> GetExpenseByIdAsync(int id)
        {
            string query = "SELECT * FROM daily_expenses WHERE expense_id = @id";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                    return MapToExpense(reader);

                _logger.LogWarning($"Expense with ID {id} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving expense with ID {id}");
                throw;
            }
        }

        public async Task<List<DailyExpense>> GetExpensesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var expenseList = new List<DailyExpense>();

            string query = @"
                SELECT * FROM daily_expenses
                WHERE date >= @startDate AND date <= @endDate
                ORDER BY date DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@startDate", startDate);
                command.Parameters.AddWithValue("@endDate", endDate);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    expenseList.Add(MapToExpense(reader));

                _logger.LogInformation($"Found {expenseList.Count} expenses between {startDate:d} and {endDate:d}");
                return expenseList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving expenses by date range");
                throw;
            }
        }

        public async Task<List<DailyExpense>> SearchExpensesAsync(string term)
        {
            var expenseList = new List<DailyExpense>();

            string query = @"
                SELECT * FROM daily_expenses
                WHERE decription LIKE @search
                ORDER BY date DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@search", $"%{term}%");
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    expenseList.Add(MapToExpense(reader));

                _logger.LogInformation($"Found {expenseList.Count} expenses matching '{term}'");
                return expenseList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching expenses with term '{term}'");
                throw;
            }
        }

        public async Task<int> GetTotalAmountAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                string query;
                MySqlParameter[]? parameters = null;

                if (startDate.HasValue && endDate.HasValue)
                {
                    query = "SELECT COALESCE(SUM(amount), 0) FROM daily_expenses WHERE date >= @startDate AND date <= @endDate";
                    parameters = new[]
                    {
                        new MySqlParameter("@startDate", startDate.Value),
                        new MySqlParameter("@endDate", endDate.Value)
                    };
                }
                else
                {
                    query = "SELECT COALESCE(SUM(amount), 0) FROM daily_expenses";
                }

                var result = await _db.ExecuteScalarAsync(query, parameters);
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total expense amount");
                throw;
            }
        }

        public async Task<DailyExpense> CreateExpenseAsync(DailyExpenseDto dto)
        {
            try
            {
                string query = @"
                    INSERT INTO daily_expenses (decription, date, amount, created_at, updated_at)
                    VALUES (@description, @date, @amount, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
                    SELECT LAST_INSERT_ID();";

                var parameters = new[]
                {
                    new MySqlParameter("@description", dto.Description ?? (object)DBNull.Value),
                    new MySqlParameter("@date", dto.Date),
                    new MySqlParameter("@amount", dto.Amount)
                };

                var expenseId = Convert.ToInt32(await _db.ExecuteScalarAsync(query, parameters));
                _logger.LogInformation($"Expense created with ID {expenseId}");

                return (await GetExpenseByIdAsync(expenseId))!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expense");
                throw;
            }
        }

        public async Task<bool> UpdateExpenseAsync(int id, DailyExpenseUpdateDto dto)
        {
            try
            {
                string query = @"
                    UPDATE daily_expenses
                    SET decription = @description,
                        date = @date,
                        amount = @amount,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE expense_id = @id";

                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@description", dto.Description ?? (object)DBNull.Value),
                    new MySqlParameter("@date", dto.Date),
                    new MySqlParameter("@amount", dto.Amount)
                };

                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Expense {id} updated successfully");
                    return true;
                }

                _logger.LogWarning($"Expense {id} not found");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating expense {id}");
                throw;
            }
        }

        public async Task<bool> DeleteExpenseAsync(int id)
        {
            string query = "DELETE FROM daily_expenses WHERE expense_id = @id";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Expense {id} deleted");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting expense {id}");
                throw;
            }
        }

        // ── Helpers ──────────────────────────────────────────────

        private DailyExpense MapToExpense(System.Data.Common.DbDataReader reader)
        {
            return new DailyExpense
            {
                ExpenseId = reader.GetInt32(reader.GetOrdinal("expense_id")),
                Description = reader.IsDBNull(reader.GetOrdinal("decription")) ? null : reader.GetString(reader.GetOrdinal("decription")),
                Date = reader.GetDateTime(reader.GetOrdinal("date")),
                Amount = reader.GetInt32(reader.GetOrdinal("amount")),
                CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : (DateTime?)DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_at")))
            };
        }
    }
}