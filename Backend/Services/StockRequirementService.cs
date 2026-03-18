using System.Data;
using HardwareStoreAPI.Data;
using HardwareStoreAPI.Models.DTOs;
using MySql.Data.MySqlClient;

namespace HardwareStoreAPI.Services
{
    public class StockRequirementService : IStockRequirementService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<StockRequirementService> _logger;

        public StockRequirementService(ILogger<StockRequirementService> logger)
        {
            _db = DatabaseHelper.Instance;
            _logger = logger;
        }

        public async Task<StockRequirementReportDto> GenerateRequirementListAsync(GenerateRequirementDto? filters = null)
        {
            var items = await GetLowStockItemsAsync(filters);

            var report = new StockRequirementReportDto
            {
                GeneratedAt = DateTime.Now,
                TotalItemsLowStock = items.Count,
                TotalEstimatedCost = items.Sum(i => i.EstimatedCost),
                Items = items
            };

            _logger.LogInformation($"Generated stock requirement list: {items.Count} items, Total Cost: Rs. {report.TotalEstimatedCost:N2}");
            return report;
        }

        public async Task<List<StockRequirementItemDto>> GetLowStockItemsAsync(GenerateRequirementDto? filters = null)
        {
            var items = new List<StockRequirementItemDto>();

            // Build WHERE clause based on filters
            var whereConditions = new List<string>
            {
                "pv.quantity_in_stock <= pv.reorder_level",  // Main condition: stock at or below reorder level
                "p.is_active = TRUE",
                "pv.is_active = TRUE"
            };

            var parameters = new List<MySqlParameter>();

            if (filters?.SupplierId.HasValue == true)
            {
                whereConditions.Add("p.supplier_id = @supplierId");
                parameters.Add(new MySqlParameter("@supplierId", filters.SupplierId.Value));
            }

            if (filters?.CategoryId.HasValue == true)
            {
                whereConditions.Add("p.category_id = @categoryId");
                parameters.Add(new MySqlParameter("@categoryId", filters.CategoryId.Value));
            }

            // Apply threshold percentage if provided
            if (filters?.StockThresholdPercentage.HasValue == true)
            {
                // Show items where current stock is below X% of reorder level
                // e.g., if reorder level is 100 and threshold is 50%, show items with stock <= 50
                whereConditions.Add("pv.quantity_in_stock <= (pv.reorder_level * @threshold / 100)");
                parameters.Add(new MySqlParameter("@threshold", filters.StockThresholdPercentage.Value));
            }

            string whereClause = string.Join(" AND ", whereConditions);

            string query = $@"
                SELECT 
                    pv.variant_id,
                    pv.product_id,
                    p.name AS product_name,
                    pv.size,
                    pv.class_type,
                    pv.quantity_in_stock AS current_stock,
                    pv.reorder_level,
                    pv.unit_of_measure,
                    p.supplier_id,
                    s.name AS supplier_name,
                    cat.value AS category
                FROM product_variants pv
                INNER JOIN products p ON pv.product_id = p.product_id
                LEFT JOIN supplier s ON p.supplier_id = s.supplier_id
                LEFT JOIN lookup cat ON p.category_id = cat.lookup_id
                WHERE {whereClause}
                ORDER BY 
                    (pv.reorder_level - pv.quantity_in_stock) DESC,  -- Most urgent first
                    p.name, 
                    pv.size";

            try
            {
                using var connection = _db.GetConnection();
                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddRange(parameters.ToArray());
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var currentStock = reader.GetDecimal("current_stock");
                    var reorderLevel = reader.GetDecimal("reorder_level");
                    //var costPrice = reader.GetDecimal("cost_price");

                    // Calculate required quantity (reorder level - current stock, minimum 0)
                    var requiredQuantity = Math.Max(0, reorderLevel - currentStock);

                    // If current stock is at or below reorder level, set required to reorder level
                    // This ensures we order enough to reach the reorder level
                    if (currentStock <= reorderLevel)
                    {
                        requiredQuantity = reorderLevel;
                    }

                    var item = new StockRequirementItemDto
                    {
                        VariantId = reader.GetInt32("variant_id"),
                        ProductId = reader.GetInt32("product_id"),
                        ProductName = reader.GetString("product_name"),
                        Size = reader.GetString("size"),
                        ClassType = reader.IsDBNull(reader.GetOrdinal("class_type"))
                            ? null
                            : reader.GetString("class_type"),
                        CurrentStock = currentStock,
                        ReorderLevel = reorderLevel,
                        RequiredQuantity = requiredQuantity,
                        UnitOfMeasure = reader.GetString("unit_of_measure"),
                        //CostPrice = costPrice,
                        //EstimatedCost = requiredQuantity * costPrice,
                        SupplierId = reader.IsDBNull(reader.GetOrdinal("supplier_id"))
                            ? null
                            : reader.GetInt32("supplier_id"),
                        SupplierName = reader.IsDBNull(reader.GetOrdinal("supplier_name"))
                            ? null
                            : reader.GetString("supplier_name"),
                        Category = reader.IsDBNull(reader.GetOrdinal("category"))
                            ? null
                            : reader.GetString("category")
                    };

                    items.Add(item);
                }

                _logger.LogInformation($"Found {items.Count} items with low stock");
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving low stock items");
                throw;
            }
        }
    }
}