using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace HardwareStoreAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly DatabaseHelper _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
        {
            _db = DatabaseHelper.Instance;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponse> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var user = await GetUserByUsernameAsync(loginDto.Username);

                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    };
                }

                if (!VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    };
                }

                if (!user.IsActive)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Your account has been deactivated"
                    };
                }

                var token = GenerateJwtToken(user);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    User = new UserInfo
                    {
                        StaffId = user.StaffId,
                        Username = user.Username,
                        Name = user.Name,
                        Email = user.Email ?? "",
                        Role = user.RoleName ?? "User"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return new AuthResponse
                {
                    Success = false,
                    Message = "An error occurred during login"
                };
            }
        }

        public async Task<AuthResponse> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                var existingUser = await GetUserByUsernameAsync(registerDto.Username);
                if (existingUser != null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Username already exists"
                    };
                }

                // Get role_id from lookup table
                int roleId = await GetRoleIdByNameAsync(registerDto.Role);
                if (roleId == 0)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid role"
                    };
                }

                var passwordHash = HashPassword(registerDto.Password);

                string query = @"
                    INSERT INTO staff (name, email, contact, cnic, address, role_id, username, password_hash, is_active, hire_date)
                    VALUES (@name, @email, @contact, @cnic, @address, @role_id, @username, @password_hash, 1, CURDATE());
                    SELECT LAST_INSERT_ID();";

                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@name", registerDto.Name);
                command.Parameters.AddWithValue("@email", registerDto.Email);
                command.Parameters.AddWithValue("@contact", registerDto.Contact ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@cnic", registerDto.Cnic ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@address", registerDto.Address ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@role_id", roleId);
                command.Parameters.AddWithValue("@username", registerDto.Username);
                command.Parameters.AddWithValue("@password_hash", passwordHash);

                var staffId = Convert.ToInt32(await command.ExecuteScalarAsync());
                var user = await GetUserByIdAsync(staffId);

                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Failed to create user"
                    };
                }

                var token = GenerateJwtToken(user);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Registration successful",
                    Token = token,
                    User = new UserInfo
                    {
                        StaffId = user.StaffId,
                        Username = user.Username,
                        Name = user.Name,
                        Email = user.Email ?? "",
                        Role = user.RoleName ?? "User"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return new AuthResponse
                {
                    Success = false,
                    Message = "An error occurred during registration"
                };
            }
        }

        public async Task<User?> GetUserByIdAsync(int staffId)
        {
            string query = @"
                SELECT 
                    s.staff_id, s.name, s.email, s.contact, s.cnic, s.address, 
                    s.role_id, s.username, s.password_hash, s.is_active, 
                    s.hire_date, s.created_at, s.updated_at,
                    l.value as role_name
                FROM staff s
                LEFT JOIN lookup l ON s.role_id = l.lookup_id
                WHERE s.staff_id = @staffId";

            using var connection = _db.GetConnection();
            await connection.OpenAsync();
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@staffId", staffId);
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapToUser((MySqlDataReader)reader);
            }

            return null;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            string query = @"
                SELECT 
                    s.staff_id, s.name, s.email, s.contact, s.cnic, s.address, 
                    s.role_id, s.username, s.password_hash, s.is_active, 
                    s.hire_date, s.created_at, s.updated_at,
                    l.value as role_name
                FROM staff s
                LEFT JOIN lookup l ON s.role_id = l.lookup_id
                WHERE s.username = @username";

            using var connection = _db.GetConnection();
            await connection.OpenAsync();
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapToUser((MySqlDataReader)reader);
            }

            return null;
        }

        public string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "1440");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.StaffId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.StaffId.ToString()),  // ✅ Added
                new Claim("StaffId", user.StaffId.ToString()),                  // ✅ Added - Custom claim for easy access
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.RoleName ?? "User"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<int> GetRoleIdByNameAsync(string roleName)
        {
            string query = "SELECT lookup_id FROM lookup WHERE type = 'user_role' AND value = @roleName LIMIT 1";

            using var connection = _db.GetConnection();
            await connection.OpenAsync();
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@roleName", roleName);

            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private User MapToUser(MySqlDataReader reader)
        {
            return new User
            {
                StaffId = reader.GetInt32("staff_id"),
                Name = reader.GetString("name"),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                Contact = reader.IsDBNull(reader.GetOrdinal("contact")) ? null : reader.GetString("contact"),
                Cnic = reader.IsDBNull(reader.GetOrdinal("cnic")) ? null : reader.GetString("cnic"),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                RoleId = reader.GetInt32("role_id"),
                RoleName = reader.IsDBNull(reader.GetOrdinal("role_name")) ? "User" : reader.GetString("role_name"),
                Username = reader.GetString("username"),
                PasswordHash = reader.GetString("password_hash"),
                IsActive = reader.GetBoolean("is_active"),
                HireDate = reader.IsDBNull(reader.GetOrdinal("hire_date")) ? null : reader.GetDateTime("hire_date"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            };
        }

        private string HashPassword(string password)
        {
            // Use bcrypt with work factor 10 (standard)
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                if (passwordHash.StartsWith("$2a$") ||
                    passwordHash.StartsWith("$2b$") ||
                    passwordHash.StartsWith("$2y$"))
                {
                    // Normalize $2y$ to $2b$ for .NET BCrypt compatibility
                    var normalizedHash = passwordHash.StartsWith("$2y$")
                        ? "$2b$" + passwordHash.Substring(4)
                        : passwordHash;

                    return BCrypt.Net.BCrypt.Verify(password, normalizedHash);
                }
                else
                {
                    // Plain text fallback
                    return password == passwordHash;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}