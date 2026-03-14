// Services/ICategoryService.cs
using HardwareStoreAPI.Models;
using HardwareStoreAPI.Models.DTOs;

namespace HardwareStoreAPI.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllCategoriesAsync(bool includeInactive = false);
        Task<PaginatedResponse<Category>> GetCategoriesPaginatedAsync(int pageNumber, int pageSize, bool includeInactive = false);
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category> CreateCategoryAsync(CategoryDto1 categoryDto);
        Task<bool> UpdateCategoryAsync(int id, CategoryUpdateDto categoryDto);
        Task<bool> DeleteCategoryAsync(int id);
        Task<bool> RestoreCategoryAsync(int id);
        Task<List<Category>> SearchCategoriesAsync(string searchTerm, bool includeInactive = false);
    }
}