using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface IStockRequirementService
    {
        Task<StockRequirementReportDto> GenerateRequirementListAsync(GenerateRequirementDto? filters = null);
        Task<List<StockRequirementItemDto>> GetLowStockItemsAsync(GenerateRequirementDto? filters = null);
    }
}