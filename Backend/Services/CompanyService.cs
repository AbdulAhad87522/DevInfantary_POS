using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using MySql.Data.MySqlClient;

namespace HardwareStoreAPI.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<CompanyService> _logger;

        public CompanyService(DatabaseHelper db, ILogger<CompanyService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<Company>> GetAllAsync(bool includeInactive = false)
        {
            var companies = new List<Company>();
            string query = includeInactive
                ? "SELECT * FROM supplier ORDER BY name"
                : "SELECT * FROM supplier WHERE is_active = 1 ORDER BY name";

            using var connection = _db.GetConnection();
            await connection.OpenAsync();
            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                companies.Add(Map(reader));

            return companies;
        }

        public async Task<Company?> GetByIdAsync(int id)
        {
            string query = "SELECT * FROM supplier WHERE supplier_id = @id";

            using var connection = _db.GetConnection();
            await connection.OpenAsync();
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return Map(reader);

            return null;
        }

        public async Task<Company> CreateAsync(CompanyDto dto)
        {
            string query = @"
                INSERT INTO supplier (name, contact, address, is_active)
                VALUES (@name, @contact, @address, 1);
                SELECT LAST_INSERT_ID();";

            var parameters = new[]
            {
                new MySqlParameter("@name", dto.CompanyName),
                new MySqlParameter("@contact", dto.Contact ?? (object)DBNull.Value),
                new MySqlParameter("@address", dto.Address ?? (object)DBNull.Value)
            };

            var id = Convert.ToInt32(await _db.ExecuteScalarAsync(query, parameters));
            return (await GetByIdAsync(id))!;
        }

        public async Task<bool> UpdateAsync(int id, CompanyUpdateDto dto)
        {
            string query = @"
                UPDATE supplier
                SET name = @name,
                    contact = @contact,
                    address = @address,
                    updated_at = CURRENT_TIMESTAMP
                WHERE supplier_id = @id AND is_active = 1";

            var parameters = new[]
            {
                new MySqlParameter("@id", id),
                new MySqlParameter("@name", dto.CompanyName),
                new MySqlParameter("@contact", dto.Contact ?? (object)DBNull.Value),
                new MySqlParameter("@address", dto.Address ?? (object)DBNull.Value)
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            string query = "UPDATE supplier SET is_active = 0 WHERE supplier_id = @id";
            var parameters = new[] { new MySqlParameter("@id", id) };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> RestoreAsync(int id)
        {
            string query = "UPDATE supplier SET is_active = 1 WHERE supplier_id = @id";
            var parameters = new[] { new MySqlParameter("@id", id) };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        private Company Map(System.Data.Common.DbDataReader reader)
        {
            return new Company
            {
                CompanyId = reader.GetInt32(reader.GetOrdinal("supplier_id")),
                CompanyName = reader.GetString(reader.GetOrdinal("name")),
                Contact = reader.IsDBNull(reader.GetOrdinal("contact")) ? null : reader.GetString(reader.GetOrdinal("contact")),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }
    }
}
