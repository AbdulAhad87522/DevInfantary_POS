using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using MySql.Data.MySqlClient;
using System.Data.Common;

namespace HardwareStoreAPI.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<SupplierService> _logger;

        public SupplierService(ILogger<SupplierService> logger)
        {
            _db = DatabaseHelper.Instance;
            _logger = logger;
        }

        public async Task<List<Supplier>> GetAllSuppliersAsync(bool includeInactive = false)
        {
            var suppliers = new List<Supplier>();
            string query = includeInactive
                ? "SELECT * FROM supplier ORDER BY name"
                : "SELECT * FROM supplier WHERE is_active = 1 ORDER BY name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    suppliers.Add(MapToSupplier(reader));
                }

                _logger.LogInformation($"Retrieved {suppliers.Count} suppliers");
                return suppliers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all suppliers");
                throw;
            }
        }

        public async Task<PaginatedResponse<Supplier>> GetSuppliersPaginatedAsync(int pageNumber, int pageSize, bool includeInactive = false)
        {
            var response = new PaginatedResponse<Supplier>
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            try
            {
                string whereClause = includeInactive ? "" : "WHERE is_active = 1";

                // Get total count
                string countQuery = $"SELECT COUNT(*) FROM supplier {whereClause}";
                response.TotalRecords = Convert.ToInt32(await _db.ExecuteScalarAsync(countQuery));

                // Get paginated data
                int offset = (pageNumber - 1) * pageSize;
                string query = $@"
                    SELECT * FROM supplier 
                    {whereClause}
                    ORDER BY name 
                    LIMIT @pageSize OFFSET @offset";

                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@pageSize", pageSize);
                command.Parameters.AddWithValue("@offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    response.Data.Add(MapToSupplier(reader));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated suppliers");
                throw;
            }
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int id)
        {
            string query = "SELECT * FROM supplier WHERE supplier_id = @id";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return MapToSupplier(reader);
                }

                _logger.LogWarning($"Supplier with ID {id} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving supplier with ID {id}");
                throw;
            }
        }

       public async Task<Supplier> CreateSupplierAsync(SupplierDto supplierDto)
{
    string query = @"
        INSERT INTO supplier (name, contact, address, account_balance, notes, is_active, created_at, updated_at)
        VALUES (@name, @contact, @address, @account_balance, @notes, 1, NOW(), NOW());
        SELECT LAST_INSERT_ID();";

    try
    {
        // Log what we're trying to save
        _logger.LogInformation($"Attempting to create supplier with name: {supplierDto.Name}, initial balance: {supplierDto.InitialBalance}");
        
        var parameters = new[]
        {
            new MySqlParameter("@name", supplierDto.Name),
            new MySqlParameter("@contact", supplierDto.Contact ?? (object)DBNull.Value),
            new MySqlParameter("@address", supplierDto.Address ?? (object)DBNull.Value),
            new MySqlParameter("@account_balance", supplierDto.InitialBalance ?? 0),
            new MySqlParameter("@notes", supplierDto.Notes ?? (object)DBNull.Value)
        };

        // Log the parameter values
        _logger.LogInformation($"Parameters: account_balance = {supplierDto.InitialBalance ?? 0}");

        var supplierId = Convert.ToInt32(await _db.ExecuteScalarAsync(query, parameters));
        _logger.LogInformation($"Supplier created with ID {supplierId} with initial balance: {supplierDto.InitialBalance ?? 0}");

        return (await GetSupplierByIdAsync(supplierId))!;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating supplier");
        throw;
    }
}

        public async Task<bool> UpdateSupplierAsync(int id, SupplierUpdateDto supplierDto)
        {
            string query = @"
                UPDATE supplier 
                SET name = @name, 
                    contact = @contact, 
                    address = @address,
                    notes = @notes,
                    updated_at = CURRENT_TIMESTAMP
                WHERE supplier_id = @id AND is_active = 1";

            try
            {
                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@name", supplierDto.Name),
                    new MySqlParameter("@contact", supplierDto.Contact ?? (object)DBNull.Value),
                    new MySqlParameter("@address", supplierDto.Address ?? (object)DBNull.Value),
                    new MySqlParameter("@notes", supplierDto.Notes ?? (object)DBNull.Value)
                };

                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Supplier {id} updated successfully");
                    return true;
                }

                _logger.LogWarning($"Supplier {id} not found or inactive");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating supplier {id}");
                throw;
            }
        }

        public async Task<bool> DeleteSupplierAsync(int id)
        {
            // Soft delete
            string query = "UPDATE supplier SET is_active = 0, updated_at = CURRENT_TIMESTAMP WHERE supplier_id = @id";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Supplier {id} deleted (soft delete)");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting supplier {id}");
                throw;
            }
        }

        public async Task<bool> RestoreSupplierAsync(int id)
        {
            string query = "UPDATE supplier SET is_active = 1, updated_at = CURRENT_TIMESTAMP WHERE supplier_id = @id";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Supplier {id} restored");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring supplier {id}");
                throw;
            }
        }

        public async Task<List<Supplier>> SearchSuppliersAsync(string searchTerm, bool includeInactive = false)
        {
            var suppliers = new List<Supplier>();
            string activeClause = includeInactive ? "" : "AND is_active = 1";

            string query = $@"
                SELECT * FROM supplier 
                WHERE (name LIKE @search 
                   OR contact LIKE @search 
                   OR address LIKE @search)
                {activeClause}
                ORDER BY name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@search", $"%{searchTerm}%");
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    suppliers.Add(MapToSupplier(reader));
                }

                _logger.LogInformation($"Found {suppliers.Count} suppliers matching '{searchTerm}'");
                return suppliers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching suppliers with term '{searchTerm}'");
                throw;
            }
        }

        public async Task<decimal> GetSupplierBalanceAsync(int id)
        {
            string query = "SELECT account_balance FROM supplier WHERE supplier_id = @id";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var result = await _db.ExecuteScalarAsync(query, parameters);

                return result != null ? Convert.ToDecimal(result) : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving balance for supplier {id}");
                throw;
            }
        }

        public async Task<bool> UpdateSupplierBalanceAsync(int id, decimal amount)
        {
            string query = @"
                UPDATE supplier 
                SET account_balance = account_balance + @amount,
                    updated_at = CURRENT_TIMESTAMP
                WHERE supplier_id = @id";

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
                    _logger.LogInformation($"Updated balance for supplier {id} by {amount}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating balance for supplier {id}");
                throw;
            }
        }

        public async Task<List<Supplier>> GetSuppliersWithBalanceAsync()
        {
            var suppliers = new List<Supplier>();
            string query = @"
                SELECT * FROM supplier 
                WHERE is_active = 1 AND account_balance > 0 
                ORDER BY account_balance DESC";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    suppliers.Add(MapToSupplier(reader));
                }

                _logger.LogInformation($"Retrieved {suppliers.Count} suppliers with outstanding balance");
                return suppliers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers with balance");
                throw;
            }
        }

        private Supplier MapToSupplier(DbDataReader reader)
        {
            return new Supplier
            {
                SupplierId = reader.GetInt32(reader.GetOrdinal("supplier_id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Contact = reader.IsDBNull(reader.GetOrdinal("contact")) ? null : reader.GetString(reader.GetOrdinal("contact")),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                AccountBalance = reader.GetDecimal(reader.GetOrdinal("account_balance")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes"))
            };
        }
    }
}