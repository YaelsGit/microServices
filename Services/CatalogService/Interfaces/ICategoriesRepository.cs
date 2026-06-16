using SharedModels.Models;

namespace CatalogService.Interfaces;

/// <summary>
/// Repository interface for Category data access in CatalogService
/// </summary>
public interface ICategoriesRepository
{
    Task<Category?> GetCategoryByIdAsync(int categoryId);
    Task<List<Category>> GetAllCategoriesAsync();
    Task<int> CreateCategoryAsync(Category category);
    Task<bool> UpdateCategoryAsync(Category category);
    Task<bool> DeleteCategoryAsync(int categoryId);
    Task<Category?> GetCategoryByNameAsync(string name);
}
