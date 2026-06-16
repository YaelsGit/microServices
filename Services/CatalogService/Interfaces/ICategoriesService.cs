using SharedModels.Dtos;

namespace CatalogService.Interfaces;

/// <summary>
/// Service interface for Category business logic
/// Defines contracts for category operations
/// </summary>
public interface ICategoriesService
{
    Task<IEnumerable<DtoCategories>> GetAllCategoriesAsync();
    Task<DtoCategories?> GetCategoryByIdAsync(int categoryId);
    Task<int> CreateCategoryAsync(DtoCreateCategoryRequest request);
    Task<bool> UpdateCategoryAsync(int categoryId, DtoUpdateCategoryRequest request);
    Task<bool> DeleteCategoryAsync(int categoryId);
}
