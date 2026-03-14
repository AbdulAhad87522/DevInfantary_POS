// Services/StaffService.cs
using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;

namespace HardwareStoreAPI.Services
{
    public class StaffService : IStaffService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<StaffService> _logger;

        public StaffService(ILogger<StaffService> logger)
        {
            _db = DatabaseHelper.Instance;
            _logger = logger;
        }

        public async Task<List<Staff>> GetAllStaffAsync(bool includeInactive = false)
        {
            var staffList = new List<Staff>();
            string activeClause = includeInactive ? "" : "AND s.is_active = 1";

            string query = $@"
                SELECT s.*, l.value AS role_name 
                FROM staff s
                INNER JOIN lookup l ON s.role_id = l.lookup_id AND l.type = 'user_role'
                WHERE 1=1 {activeClause}
                ORDER BY s.name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    staffList.Add(MapToStaff(reader));

                _logger.LogInformation($"Retrieved {staffList.Count} staff members");
                return staffList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all staff");
                throw;
            }
        }

        public async Task<PaginatedResponse<Staff>> GetStaffPaginatedAsync(int pageNumber, int pageSize, bool includeInactive = false)
        {
            var response = new PaginatedResponse<Staff>
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            try
            {
                string activeClause = includeInactive ? "" : "AND s.is_active = 1";

                string countQuery = $@"
                    SELECT COUNT(*) FROM staff s
                    INNER JOIN lookup l ON s.role_id = l.lookup_id AND l.type = 'user_role'
                    WHERE 1=1 {activeClause}";

                response.TotalRecords = Convert.ToInt32(await _db.ExecuteScalarAsync(countQuery));

                int offset = (pageNumber - 1) * pageSize;
                string query = $@"
                    SELECT s.*, l.value AS role_name 
                    FROM staff s
                    INNER JOIN lookup l ON s.role_id = l.lookup_id AND l.type = 'user_role'
                    WHERE 1=1 {activeClause}
                    ORDER BY s.name
                    LIMIT @pageSize OFFSET @offset";

                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@pageSize", pageSize);
                command.Parameters.AddWithValue("@offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    response.Data.Add(MapToStaff(reader));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated staff");
                throw;
            }
        }

        public async Task<Staff?> GetStaffByIdAsync(int id)
        {
            string query = @"
                SELECT s.*, l.value AS role_name 
                FROM staff s
                INNER JOIN lookup l ON s.role_id = l.lookup_id AND l.type = 'user_role'
                WHERE s.staff_id = @id";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                    return MapToStaff(reader);

                _logger.LogWarning($"Staff with ID {id} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving staff with ID {id}");
                throw;
            }
        }

        public async Task<Staff> CreateStaffAsync(StaffDto staffDto)
        {
            try
            {
                // Fetch role_id from lookup table
                int roleId = await GetRoleIdAsync(staffDto.RoleName);

                string query = @"
                    INSERT INTO staff (name, email, contact, cnic, address, role_id, username, password_hash, is_active, hire_date)
                    VALUES (@name, @email, @contact, @cnic, @address, @roleId, @username, @passwordHash, 1, @hireDate);
                    SELECT LAST_INSERT_ID();";

                var parameters = new[]
                {
                    new MySqlParameter("@name", staffDto.Name),
                    new MySqlParameter("@email", staffDto.Email ?? (object)DBNull.Value),
                    new MySqlParameter("@contact", staffDto.Contact ?? (object)DBNull.Value),
                    new MySqlParameter("@cnic", staffDto.Cnic ?? (object)DBNull.Value),
                    new MySqlParameter("@address", staffDto.Address ?? (object)DBNull.Value),
                    new MySqlParameter("@roleId", roleId),
                    new MySqlParameter("@username", staffDto.Username),
                    new MySqlParameter("@passwordHash", HashPassword(staffDto.Password)),
                    new MySqlParameter("@hireDate", staffDto.HireDate ?? (object)DBNull.Value)
                };

                var staffId = Convert.ToInt32(await _db.ExecuteScalarAsync(query, parameters));
                _logger.LogInformation($"Staff created with ID {staffId}");

                return (await GetStaffByIdAsync(staffId))!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating staff");
                throw;
            }
        }

        public async Task<bool> UpdateStaffAsync(int id, StaffUpdateDto staffDto)
        {
            try
            {
                // Fetch role_id from lookup table
                int roleId = await GetRoleIdAsync(staffDto.RoleName);

                string query = @"
                    UPDATE staff 
                    SET name = @name,
                        email = @email,
                        contact = @contact,
                        cnic = @cnic,
                        address = @address,
                        role_id = @roleId,
                        username = COALESCE(@username, username),
                        hire_date = @hireDate,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE staff_id = @id AND is_active = 1";

                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@name", staffDto.Name),
                    new MySqlParameter("@email", staffDto.Email ?? (object)DBNull.Value),
                    new MySqlParameter("@contact", staffDto.Contact ?? (object)DBNull.Value),
                    new MySqlParameter("@cnic", staffDto.Cnic ?? (object)DBNull.Value),
                    new MySqlParameter("@address", staffDto.Address ?? (object)DBNull.Value),
                    new MySqlParameter("@roleId", roleId),
                    new MySqlParameter("@username", staffDto.Username ?? (object)DBNull.Value),
                    new MySqlParameter("@hireDate", staffDto.HireDate ?? (object)DBNull.Value)
                };

                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Staff {id} updated successfully");
                    return true;
                }

                _logger.LogWarning($"Staff {id} not found or inactive");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating staff {id}");
                throw;
            }
        }

        public async Task<bool> DeleteStaffAsync(int id)
        {
            string query = "UPDATE staff SET is_active = 0, updated_at = CURRENT_TIMESTAMP WHERE staff_id = @id";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Staff {id} deleted (soft delete)");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting staff {id}");
                throw;
            }
        }

        public async Task<bool> RestoreStaffAsync(int id)
        {
            string query = "UPDATE staff SET is_active = 1, updated_at = CURRENT_TIMESTAMP WHERE staff_id = @id";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Staff {id} restored");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring staff {id}");
                throw;
            }
        }

        public async Task<List<Staff>> SearchStaffAsync(string searchTerm, bool includeInactive = false)
        {
            var staffList = new List<Staff>();
            string activeClause = includeInactive ? "" : "AND s.is_active = 1";

            string query = $@"
                SELECT s.*, l.value AS role_name 
                FROM staff s
                INNER JOIN lookup l ON s.role_id = l.lookup_id AND l.type = 'user_role'
                WHERE (s.name LIKE @search 
                   OR s.email LIKE @search 
                   OR s.contact LIKE @search
                   OR s.username LIKE @search
                   OR s.cnic LIKE @search)
                {activeClause}
                ORDER BY s.name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@search", $"%{searchTerm}%");
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    staffList.Add(MapToStaff(reader));

                _logger.LogInformation($"Found {staffList.Count} staff matching '{searchTerm}'");
                return staffList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching staff with term '{searchTerm}'");
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(int id, StaffChangePasswordDto dto)
        {
            try
            {
                // Verify current password first
                string verifyQuery = "SELECT password_hash FROM staff WHERE staff_id = @id AND is_active = 1";
                var parameters = new[] { new MySqlParameter("@id", id) };
                var currentHash = await _db.ExecuteScalarAsync(verifyQuery, parameters);

                if (currentHash == null || currentHash.ToString() != HashPassword(dto.CurrentPassword))
                {
                    _logger.LogWarning($"Invalid current password for staff {id}");
                    return false;
                }

                string updateQuery = @"
                    UPDATE staff 
                    SET password_hash = @newHash, updated_at = CURRENT_TIMESTAMP
                    WHERE staff_id = @id AND is_active = 1";

                var updateParams = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@newHash", HashPassword(dto.NewPassword))
                };

                var rowsAffected = await _db.ExecuteNonQueryAsync(updateQuery, updateParams);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Password changed for staff {id}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error changing password for staff {id}");
                throw;
            }
        }

        public async Task<List<Staff>> GetStaffByRoleAsync(string roleName)
        {
            var staffList = new List<Staff>();

            string query = @"
                SELECT s.*, l.value AS role_name 
                FROM staff s
                INNER JOIN lookup l ON s.role_id = l.lookup_id AND l.type = 'user_role'
                WHERE l.value = @roleName AND s.is_active = 1
                ORDER BY s.name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@roleName", roleName);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    staffList.Add(MapToStaff(reader));

                _logger.LogInformation($"Found {staffList.Count} staff with role '{roleName}'");
                return staffList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving staff by role '{roleName}'");
                throw;
            }
        }

        // ── Helpers ──────────────────────────────────────────────

        private async Task<int> GetRoleIdAsync(string roleName)
        {
            string query = "SELECT lookup_id FROM lookup WHERE type = 'user_role' AND value = @roleName AND is_active = 1";
            var parameters = new[] { new MySqlParameter("@roleName", roleName) };
            var result = await _db.ExecuteScalarAsync(query, parameters);

            if (result == null)
                throw new Exception($"Role '{roleName}' not found in lookup table");

            return Convert.ToInt32(result);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private Staff MapToStaff(System.Data.Common.DbDataReader reader)
        {
            return new Staff
            {
                StaffId = reader.GetInt32(reader.GetOrdinal("staff_id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                Contact = reader.IsDBNull(reader.GetOrdinal("contact")) ? null : reader.GetString(reader.GetOrdinal("contact")),
                Cnic = reader.IsDBNull(reader.GetOrdinal("cnic")) ? null : reader.GetString(reader.GetOrdinal("cnic")),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                RoleId = reader.GetInt32(reader.GetOrdinal("role_id")),
                RoleName = reader.GetString(reader.GetOrdinal("role_name")),
                Username = reader.GetString(reader.GetOrdinal("username")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                HireDate = reader.IsDBNull(reader.GetOrdinal("hire_date")) ? null : reader.GetDateTime(reader.GetOrdinal("hire_date")),
                CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
                LastLogin = reader.IsDBNull(reader.GetOrdinal("last_login")) ? null : reader.GetDateTime(reader.GetOrdinal("last_login"))
            };
        }
    }
}