using SharedModels.Models;
using SharedModels.Dtos;
using SharedModels.Constants;
using CatalogService.Interfaces;

namespace CatalogService.Services;

/// <summary>
/// Service implementation for Category business logic
/// Handles CRUD operations and category-related business rules
/// </summary>
public class CategoriesService : ICategoriesService
{
    private readonly ICategoriesRepository _repository;
    private readonly ILogger<CategoriesService> _logger;

    public CategoriesService(ICategoriesRepository repository, ILogger<CategoriesService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    public async Task<IEnumerable<DtoCategories>> GetAllCategoriesAsync()
    {
        try
        {
            var categories = await _repository.GetAllCategoriesAsync();
            return categories.Select(c => MapToDto(c));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all categories");
            throw;
        }
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    public async Task<DtoCategories?> GetCategoryByIdAsync(int categoryId)
    {
        try
        {
            if (categoryId <= 0)
                return null;

            var category = await _repository.GetCategoryByIdAsync(categoryId);
            return category == null ? null : MapToDto(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting category: {categoryId}");
            throw;
        }
    }

    /// <summary>
    /// Create new category
    /// </summary>
    public async Task<int> CreateCategoryAsync(DtoCreateCategoryRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                _logger.LogWarning("Create category attempt with empty name");
                throw new ArgumentException("Category name is required");
            }

            var category = new Category
            {
                Name = request.Name,
                Description = request.Description
            };

            var categoryId = await _repository.CreateCategoryAsync(category);
            _logger.LogInformation($"New category created: {categoryId}");
            return categoryId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            throw;
        }
    }

    /// <summary>
    /// Update category
    /// </summary>
    public async Task<bool> UpdateCategoryAsync(int categoryId, DtoUpdateCategoryRequest request)
    {
        try
        {
            var category = await _repository.GetCategoryByIdAsync(categoryId);
            if (category == null)
            {
                _logger.LogWarning($"Update category attempt for non-existent category: {categoryId}");
                return false;
            }

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(request.Name))
                category.Name = request.Name;
            if (!string.IsNullOrWhiteSpace(request.Description))
                category.Description = request.Description;

            var success = await _repository.UpdateCategoryAsync(category);
            if (success)
            {
                _logger.LogInformation($"Category updated: {categoryId}");
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating category: {categoryId}");
            throw;
        }
    }

    /// <summary>
    /// Delete category
    /// </summary>
    public async Task<bool> DeleteCategoryAsync(int categoryId)
    {
        try
        {
            var success = await _repository.DeleteCategoryAsync(categoryId);
            if (success)
            {
                _logger.LogInformation($"Category deleted: {categoryId}");
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting category: {categoryId}");
            throw;
        }
    }

    /// <summary>
    /// Helper method to map Category to DtoCategories
    /// </summary>
    private DtoCategories MapToDto(Category category)
    {
        return new DtoCategories
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}
