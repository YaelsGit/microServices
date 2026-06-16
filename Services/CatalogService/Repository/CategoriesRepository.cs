using Microsoft.EntityFrameworkCore;
using SharedModels.Models;
using CatalogService.Data;
using CatalogService.Interfaces;

namespace CatalogService.Repository;

/// <summary>
/// Repository implementation for Category data access
/// Handles CRUD operations on Categories table in CatalogDbContext
/// </summary>
public class CategoriesRepository : ICategoriesRepository
{
    private readonly CatalogDbContext _context;

    public CategoriesRepository(CatalogDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    public async Task<Category?> GetCategoryByIdAsync(int categoryId)
    {
        return await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Create new category
    /// Returns the generated CategoryId
    /// </summary>
    public async Task<int> CreateCategoryAsync(Category category)
    {
        category.CreatedAt = DateTime.UtcNow;
        
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        
        return category.CategoryId;
    }

    /// <summary>
    /// Update existing category
    /// </summary>
    public async Task<bool> UpdateCategoryAsync(Category category)
    {
        category.UpdatedAt = DateTime.UtcNow;
        
        _context.Categories.Update(category);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Delete category by ID
    /// </summary>
    public async Task<bool> DeleteCategoryAsync(int categoryId)
    {
        var category = await _context.Categories.FindAsync(categoryId);
        if (category == null)
            return false;

        _context.Categories.Remove(category);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Get category by name
    /// </summary>
    public async Task<Category?> GetCategoryByNameAsync(string name)
    {
        return await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name);
    }
}
