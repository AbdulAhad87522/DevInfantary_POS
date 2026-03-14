// Services/CategoryService.cs
using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;
using MySql.Data.MySqlClient;

namespace HardwareStoreAPI.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ILogger<CategoryService> logger)
        {
            _db = DatabaseHelper.Instance;
            _logger = logger;
        }

        public async Task<List<Category>> GetAllCategoriesAsync(bool includeInactive = false)
        {
            var categories = new List<Category>();
            string query = includeInactive
                ? "SELECT * FROM lookup WHERE type = 'category' ORDER BY display_order"
                : "SELECT * FROM lookup WHERE type = 'category' AND is_active = 1 ORDER BY display_order";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    categories.Add(MapToCategory(reader));
                }

                _logger.LogInformation($"Retrieved {categories.Count} categories");
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all categories");
                throw;
            }
        }

        public async Task<PaginatedResponse<Category>> GetCategoriesPaginatedAsync(int pageNumber, int pageSize, bool includeInactive = false)
        {
            var response = new PaginatedResponse<Category>
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            try
            {
                string whereClause = includeInactive
                    ? "WHERE type = 'category'"
                    : "WHERE type = 'category' AND is_active = 1";

                string countQuery = $"SELECT COUNT(*) FROM lookup {whereClause}";
                response.TotalRecords = Convert.ToInt32(await _db.ExecuteScalarAsync(countQuery));

                int offset = (pageNumber - 1) * pageSize;
                string query = $@"
                    SELECT * FROM lookup 
                    {whereClause}
                    ORDER BY display_order 
                    LIMIT @pageSize OFFSET @offset";

                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@pageSize", pageSize);
                command.Parameters.AddWithValue("@offset", offset);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    response.Data.Add(MapToCategory(reader));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated categories");
                throw;
            }
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            string query = "SELECT * FROM lookup WHERE lookup_id = @id AND type = 'category'";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return MapToCategory(reader);
                }

                _logger.LogWarning($"Category with ID {id} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving category with ID {id}");
                throw;
            }
        }

        public async Task<Category> CreateCategoryAsync(CategoryDto1 categoryDto)
        {
            string query = @"
                INSERT INTO lookup (type, value, description, display_order, is_active)
                VALUES ('category', @value, @description, @displayOrder, 1);
                SELECT LAST_INSERT_ID();";

            try
            {
                var parameters = new[]
                {
                    new MySqlParameter("@value", categoryDto.Value),
                    new MySqlParameter("@description", categoryDto.Description ?? (object)DBNull.Value),
                    new MySqlParameter("@displayOrder", categoryDto.DisplayOrder ?? (object)DBNull.Value)
                };

                var categoryId = Convert.ToInt32(await _db.ExecuteScalarAsync(query, parameters));
                _logger.LogInformation($"Category created with ID {categoryId}");

                return (await GetCategoryByIdAsync(categoryId))!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                throw;
            }
        }

        public async Task<bool> UpdateCategoryAsync(int id, CategoryUpdateDto categoryDto)
        {
            string query = @"
                UPDATE lookup 
                SET value = @value, 
                    description = @description, 
                    display_order = @displayOrder
                WHERE lookup_id = @id AND type = 'category' AND is_active = 1";

            try
            {
                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@value", categoryDto.Value),
                    new MySqlParameter("@description", categoryDto.Description ?? (object)DBNull.Value),
                    new MySqlParameter("@displayOrder", categoryDto.DisplayOrder ?? (object)DBNull.Value)
                };

                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Category {id} updated successfully");
                    return true;
                }

                _logger.LogWarning($"Category {id} not found or inactive");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating category {id}");
                throw;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            string query = "UPDATE lookup SET is_active = 0 WHERE lookup_id = @id AND type = 'category'";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Category {id} deleted (soft delete)");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting category {id}");
                throw;
            }
        }

        public async Task<bool> RestoreCategoryAsync(int id)
        {
            string query = "UPDATE lookup SET is_active = 1 WHERE lookup_id = @id AND type = 'category'";

            try
            {
                var parameters = new[] { new MySqlParameter("@id", id) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"Category {id} restored");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring category {id}");
                throw;
            }
        }

        public async Task<List<Category>> SearchCategoriesAsync(string searchTerm, bool includeInactive = false)
        {
            var categories = new List<Category>();
            string activeClause = includeInactive ? "" : "AND is_active = 1";

            string query = $@"
                SELECT * FROM lookup 
                WHERE type = 'category'
                AND (value LIKE @search OR description LIKE @search)
                {activeClause}
                ORDER BY display_order";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@search", $"%{searchTerm}%");
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    categories.Add(MapToCategory(reader));
                }

                _logger.LogInformation($"Found {categories.Count} categories matching '{searchTerm}'");
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching categories with term '{searchTerm}'");
                throw;
            }
        }

        private Category MapToCategory(System.Data.Common.DbDataReader reader)
        {
            return new Category
            {
                LookupId = reader.GetInt32(reader.GetOrdinal("lookup_id")),
                Type = reader.GetString(reader.GetOrdinal("type")),
                Value = reader.GetString(reader.GetOrdinal("value")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                DisplayOrder = reader.IsDBNull(reader.GetOrdinal("display_order")) ? null : reader.GetInt32(reader.GetOrdinal("display_order")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetDateTime(reader.GetOrdinal("created_at"))
            };
        }
    }
}