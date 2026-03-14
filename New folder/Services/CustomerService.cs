using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using MySql.Data.MySqlClient;

namespace HardwareStoreAPI.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(ILogger<CustomerService> logger)
        {
            _db = DatabaseHelper.Instance;
            _logger = logger;
        }

        public async Task<List<Customer>> GetAllCustomersAsync(bool includeInactive = false)
        {
            var customers = new List<Customer>();
            string query = includeInactive
                ? "SELECT * FROM customers ORDER BY full_name"
                : "SELECT * FROM customers WHERE is_active = 1 ORDER BY full_name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    customers.Add(MapToCustomer(reader));
                }

                _logger.LogInformation($"Retrieved {customers.Count} customers");
                return customers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all customers");
                throw;
            }
        }

        public async Task<PaginatedResponse<Customer>> GetCustomersPaginatedAsync(int pageNumber, int pageSize, bool includeInactive = false)
        {
            var response = new PaginatedResponse<Customer>
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            try
            {
                string whereClause = includeInactive ? "" : "WHERE is_active = 1";

                // Get total count
                string countQuery = $"SELECT COUNT(*) FROM customers {whereClause}";
                response.TotalRecords = Convert.ToInt32(await _db.ExecuteScalarAsync(countQuery));

                // Get paginated data
                int offset = (pageNumber - 1) * pageSize;
                string query = $@"
                    SELECT * FROM customers 
                    {whereClause}
                    ORDER BY full_name 
                    LIMIT @pageSize OFFSET @offset";

                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@pageSize", pageSize);
                command.Parameters.AddWithValue("@offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    response.Data.Add(MapToCustomer(reader));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated customers");
                throw;
            }
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            string query = "SELECT * FROM customers WHERE customer_id = @id";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return MapToCustomer(reader);
                }

                _logger.LogWarning($"Customer with ID {id} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving customer with ID {id}");
                throw;
            }
        }

        public async Task<Customer> CreateCustomerAsync(CustomerDto customerDto)
        {
            string query = @"
                INSERT INTO customers (full_name, phone, address, customer_type, notes, is_active, current_balance)
                VALUES (@fullName, @phone, @address, @customerType, @notes, 1, 0);
                SELECT LAST_INSERT_ID();";

            try
            {
                var parameters = new[]
                {
                    new MySqlParameter("@fullName", customerDto.FullName),
                    new MySqlParameter("@phone", customerDto.Phone ?? (object)DBNull.Value),
                    new MySqlParameter("@address", customerDto.Address ?? (object)DBNull.Value),
                    new MySqlParameter("@customerType", customerDto.CustomerType),
                    new MySqlParameter("@notes", customerDto.Notes ?? (object)DBNull.Value)
                };

                var customerId = Convert.ToInt32(await _db.ExecuteScalarAsync(query, parameters));
                _logger.LogInformation($"Customer created with ID {customerId}");

                return (await GetCustomerByIdAsync(customerId))!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                throw;
            }
        }

        public async Task<bool> UpdateCustomerAsync(int id, CustomerUpdateDto customerDto)
        {
            string query = @"
                UPDATE customers 
                SET full_name = @fullName, 
                    phone = @phone, 
                    address = @address,
                    customer_type = @customerType,
                    notes = @notes,
                    updated_at = CURRENT_TIMESTAMP
                WHERE customer_id = @id AND is_active = 1";

            try
            {
                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@fullName", customerDto.FullName),
                    new MySqlParameter("@phone", customerDto.Phone ?? (object)DBNull.Value),
                    new MySqlParameter("@address", customerDto.Address ?? (object)DBNull.Value),
                    new MySqlParameter("@customerType", customerDto.CustomerType),
                    new MySqlParameter("@notes", customerDto.Notes ?? (object)DBNull.Value)
                };

                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Customer {id} updated successfully");
                    return true;
                }

                _logger.LogWarning($"Customer {id} not found or inactive");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating customer {id}");
                throw;
            }
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            // Soft delete - just mark as inactive
            string query = "UPDATE customers SET is_active = 0, updated_at = CURRENT_TIMESTAMP WHERE customer_id = @id";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Customer {id} deleted (soft delete)");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting customer {id}");
                throw;
            }
        }

        public async Task<bool> RestoreCustomerAsync(int id)
        {
            string query = "UPDATE customers SET is_active = 1, updated_at = CURRENT_TIMESTAMP WHERE customer_id = @id";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Customer {id} restored");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring customer {id}");
                throw;
            }
        }

        public async Task<List<Customer>> SearchCustomersAsync(string searchTerm, bool includeInactive = false)
        {
            var customers = new List<Customer>();
            string activeClause = includeInactive ? "" : "AND is_active = 1";

            string query = $@"
                SELECT * FROM customers 
                WHERE (full_name LIKE @search 
                   OR phone LIKE @search 
                   OR address LIKE @search)
                {activeClause}
                ORDER BY full_name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@search", $"%{searchTerm}%");
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    customers.Add(MapToCustomer(reader));
                }

                _logger.LogInformation($"Found {customers.Count} customers matching '{searchTerm}'");
                return customers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching customers with term '{searchTerm}'");
                throw;
            }
        }

        public async Task<decimal> GetCustomerBalanceAsync(int id)
        {
            string query = "SELECT current_balance FROM customers WHERE customer_id = @id";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var result = await _db.ExecuteScalarAsync(query, parameters);

                return result != null ? Convert.ToDecimal(result) : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving balance for customer {id}");
                throw;
            }
        }

        public async Task<bool> UpdateCustomerBalanceAsync(int id, decimal amount)
        {
            string query = @"
                UPDATE customers 
                SET current_balance = current_balance + @amount,
                    updated_at = CURRENT_TIMESTAMP
                WHERE customer_id = @id";

            try
            {
                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@amount", amount)
                };

                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Updated balance for customer {id} by {amount}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating balance for customer {id}");
                throw;
            }
        }

        private Customer MapToCustomer(System.Data.Common.DbDataReader reader)
        {
            return new Customer
            {
                CustomerId = reader.GetInt32(reader.GetOrdinal("customer_id")),
                FullName = reader.GetString(reader.GetOrdinal("full_name")),
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                CurrentBalance = reader.GetDecimal(reader.GetOrdinal("current_balance")),
                CustomerType = reader.GetString(reader.GetOrdinal("customer_type")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes"))
            };
        }
    }
}