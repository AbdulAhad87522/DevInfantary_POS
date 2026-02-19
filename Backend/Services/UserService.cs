using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using MySql.Data.MySqlClient;
using System.Data.Common;
using BCrypt.Net;

namespace HardwareStoreAPI.Services
{
    public class UserService : IUserService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _configuration;

        public UserService(ILogger<UserService> logger, IConfiguration configuration)
        {
            _db = DatabaseHelper.Instance;
            _logger = logger;
            _configuration = configuration;
        }

        #region Authentication

        public async Task<UserLoginResponse?> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var user = await GetUserByUsernameAsync(loginDto.Username);

                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning($"Login failed for username: {loginDto.Username} - User not found or inactive");
                    return null;
                }

                // Verify password using BCrypt
                if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    _logger.LogWarning($"Login failed for username: {loginDto.Username} - Invalid password");
                    return null;
                }

                // Update last login
                await UpdateLastLoginAsync(user.StaffId);

                // Generate JWT token (you'll need to implement this)
                var token = GenerateJwtToken(user);
                var expiresAt = DateTime.UtcNow.AddHours(8);

                _logger.LogInformation($"User {user.Username} logged in successfully");

                return new UserLoginResponse
                {
                    StaffId = user.StaffId,
                    Name = user.Name,
                    Username = user.Username,
                    Role = user.Role,
                    Token = token,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            string query = "SELECT password_hash FROM staff WHERE staff_id = @userId AND is_active = 1";

            try
            {
                // Get current password hash
                var parameters = new[] { new MySqlParameter("@userId", userId) };
                var result = await _db.ExecuteScalarAsync(query, parameters);

                if (result == null)
                {
                    _logger.LogWarning($"User {userId} not found for password change");
                    return false;
                }

                string currentHash = result.ToString()!;

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, currentHash))
                {
                    _logger.LogWarning($"Invalid current password for user {userId}");
                    return false;
                }

                // Hash new password
                string newHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);

                // Update password
                string updateQuery = @"
                    UPDATE staff 
                    SET password_hash = @newHash, 
                        updated_at = CURRENT_TIMESTAMP 
                    WHERE staff_id = @userId";

                var updateParams = new[]
                {
                    new MySqlParameter("@userId", userId),
                    new MySqlParameter("@newHash", newHash)
                };

                var rowsAffected = await _db.ExecuteNonQueryAsync(updateQuery, updateParams);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Password changed for user {userId}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error changing password for user {userId}");
                throw;
            }
        }

        private string GenerateJwtToken(User user)
        {
            // You'll need to install: dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
            // This is a simplified version - implement full JWT token generation
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = System.Text.Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "YourSecretKeyHere_MakeItLongAndSecure!");

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("staff_id", user.StaffId.ToString()),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        #endregion

        #region CRUD Operations

        public async Task<List<User>> GetAllUsersAsync(bool includeInactive = false)
        {
            var users = new List<User>();
            string query = includeInactive
                ? "SELECT * FROM staff ORDER BY name"
                : "SELECT * FROM staff WHERE is_active = 1 ORDER BY name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    users.Add(MapToUser(reader));
                }

                _logger.LogInformation($"Retrieved {users.Count} users");
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                throw;
            }
        }

        public async Task<PaginatedResponse<User>> GetUsersPaginatedAsync(int pageNumber, int pageSize, bool includeInactive = false)
        {
            var response = new PaginatedResponse<User>
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            try
            {
                string whereClause = includeInactive ? "" : "WHERE is_active = 1";

                // Get total count
                string countQuery = $"SELECT COUNT(*) FROM staff {whereClause}";
                response.TotalRecords = Convert.ToInt32(await _db.ExecuteScalarAsync(countQuery));

                // Get paginated data
                int offset = (pageNumber - 1) * pageSize;
                string query = $@"
                    SELECT * FROM staff 
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
                    response.Data.Add(MapToUser(reader));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated users");
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            string query = "SELECT * FROM staff WHERE staff_id = @id";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return MapToUser(reader);
                }

                _logger.LogWarning($"User with ID {id} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user with ID {id}");
                throw;
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            string query = "SELECT * FROM staff WHERE username = @username";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@username", username);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return MapToUser(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user with username {username}");
                throw;
            }
        }

        public async Task<User> CreateUserAsync(CreateUserDto userDto)
        {
            // Check if username already exists
            if (await UsernameExistsAsync(userDto.Username))
            {
                throw new InvalidOperationException($"Username '{userDto.Username}' already exists");
            }

            // Check if email already exists (if provided)
            if (!string.IsNullOrEmpty(userDto.Email) && await EmailExistsAsync(userDto.Email))
            {
                throw new InvalidOperationException($"Email '{userDto.Email}' already exists");
            }

            // Hash the password
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

            string query = @"
                INSERT INTO staff (name, email, phone, address, username, password_hash, role, notes, is_active)
                VALUES (@name, @email, @phone, @address, @username, @passwordHash, @role, @notes, 1);
                SELECT LAST_INSERT_ID();";

            try
            {
                var parameters = new[]
                {
                    new MySqlParameter("@name", userDto.Name),
                    new MySqlParameter("@email", userDto.Email ?? (object)DBNull.Value),
                    new MySqlParameter("@phone", userDto.Phone ?? (object)DBNull.Value),
                    new MySqlParameter("@address", userDto.Address ?? (object)DBNull.Value),
                    new MySqlParameter("@username", userDto.Username),
                    new MySqlParameter("@passwordHash", passwordHash),
                    new MySqlParameter("@role", userDto.Role),
                    new MySqlParameter("@notes", userDto.Notes ?? (object)DBNull.Value)
                };

                var userId = Convert.ToInt32(await _db.ExecuteScalarAsync(query, parameters));
                _logger.LogInformation($"User created with ID {userId}");

                return (await GetUserByIdAsync(userId))!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto userDto)
        {
            // Check if email already exists for another user
            if (!string.IsNullOrEmpty(userDto.Email) && await EmailExistsAsync(userDto.Email, id))
            {
                throw new InvalidOperationException($"Email '{userDto.Email}' already exists");
            }

            string query = @"
                UPDATE staff 
                SET name = @name, 
                    email = @email, 
                    phone = @phone,
                    address = @address,
                    role = @role,
                    notes = @notes,
                    updated_at = CURRENT_TIMESTAMP
                WHERE staff_id = @id AND is_active = 1";

            try
            {
                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@name", userDto.Name),
                    new MySqlParameter("@email", userDto.Email ?? (object)DBNull.Value),
                    new MySqlParameter("@phone", userDto.Phone ?? (object)DBNull.Value),
                    new MySqlParameter("@address", userDto.Address ?? (object)DBNull.Value),
                    new MySqlParameter("@role", userDto.Role),
                    new MySqlParameter("@notes", userDto.Notes ?? (object)DBNull.Value)
                };

                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"User {id} updated successfully");
                    return true;
                }

                _logger.LogWarning($"User {id} not found or inactive");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user {id}");
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            // Soft delete
            string query = "UPDATE staff SET is_active = 0, updated_at = CURRENT_TIMESTAMP WHERE staff_id = @id";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"User {id} deleted (soft delete)");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user {id}");
                throw;
            }
        }

        public async Task<bool> RestoreUserAsync(int id)
        {
            string query = "UPDATE staff SET is_active = 1, updated_at = CURRENT_TIMESTAMP WHERE staff_id = @id";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"User {id} restored");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring user {id}");
                throw;
            }
        }

        #endregion

        #region Search & Filters

        public async Task<List<User>> SearchUsersAsync(string searchTerm, bool includeInactive = false)
        {
            var users = new List<User>();
            string activeClause = includeInactive ? "" : "AND is_active = 1";

            string query = $@"
                SELECT * FROM staff 
                WHERE (name LIKE @search 
                   OR username LIKE @search 
                   OR email LIKE @search 
                   OR phone LIKE @search)
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
                    users.Add(MapToUser(reader));
                }

                _logger.LogInformation($"Found {users.Count} users matching '{searchTerm}'");
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching users with term '{searchTerm}'");
                throw;
            }
        }

        public async Task<List<User>> GetUsersByRoleAsync(string role)
        {
            var users = new List<User>();
            string query = "SELECT * FROM staff WHERE role = @role AND is_active = 1 ORDER BY name";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@role", role);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    users.Add(MapToUser(reader));
                }

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving users with role {role}");
                throw;
            }
        }

        #endregion

        #region Status Management

        public async Task<bool> UpdateUserStatusAsync(int id, bool isActive)
        {
            string query = "UPDATE staff SET is_active = @isActive, updated_at = CURRENT_TIMESTAMP WHERE staff_id = @id";

            try
            {
                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@isActive", isActive)
                };

                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"User {id} status updated to {isActive}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating status for user {id}");
                throw;
            }
        }

        public async Task<bool> UpdateLastLoginAsync(int id)
        {
            string query = "UPDATE staff SET last_login = CURRENT_TIMESTAMP WHERE staff_id = @id";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating last login for user {id}");
                return false;
            }
        }

        #endregion

        #region Validation

        public async Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null)
        {
            string query = excludeUserId.HasValue
                ? "SELECT COUNT(*) FROM staff WHERE username = @username AND staff_id != @excludeId"
                : "SELECT COUNT(*) FROM staff WHERE username = @username";

            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter("@username", username)
            };

            if (excludeUserId.HasValue)
            {
                parameters.Add(new MySqlParameter("@excludeId", excludeUserId.Value));
            }

            var count = Convert.ToInt32(await _db.ExecuteScalarAsync(query, parameters.ToArray()));
            return count > 0;
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
        {
            if (string.IsNullOrEmpty(email)) return false;

            string query = excludeUserId.HasValue
                ? "SELECT COUNT(*) FROM staff WHERE email = @email AND staff_id != @excludeId"
                : "SELECT COUNT(*) FROM staff WHERE email = @email";

            var parameters = new List<MySqlParameter>
            {
                new MySqlParameter("@email", email)
            };

            if (excludeUserId.HasValue)
            {
                parameters.Add(new MySqlParameter("@excludeId", excludeUserId.Value));
            }

            var count = Convert.ToInt32(await _db.ExecuteScalarAsync(query, parameters.ToArray()));
            return count > 0;
        }

        #endregion

        #region Helper Methods

        private User MapToUser(DbDataReader reader)
        {
            return new User
            {
                StaffId = reader.GetInt32(reader.GetOrdinal("staff_id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                Username = reader.GetString(reader.GetOrdinal("username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                Role = reader.GetString(reader.GetOrdinal("role")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                LastLogin = reader.IsDBNull(reader.GetOrdinal("last_login")) ? null : reader.GetDateTime(reader.GetOrdinal("last_login")),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes"))
            };
        }

        #endregion
    }
}