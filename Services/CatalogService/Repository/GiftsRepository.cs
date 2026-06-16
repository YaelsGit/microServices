using Microsoft.EntityFrameworkCore;
using SharedModels.Models;
using CatalogService.Data;
using CatalogService.Interfaces;

namespace CatalogService.Repository;

/// <summary>
/// Repository implementation for Gift data access
/// Handles CRUD operations and filtering for Gifts
/// </summary>
public class GiftsRepository : IGiftsRepository
{
    private readonly CatalogDbContext _context;

    public GiftsRepository(CatalogDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get gift by ID
    /// </summary>
    public async Task<Gift?> GetGiftByIdAsync(int giftId)
    {
        return await _context.Gifts
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.GiftId == giftId);
    }

    /// <summary>
    /// Get all gifts
    /// </summary>
    public async Task<List<Gift>> GetAllGiftsAsync()
    {
        return await _context.Gifts
            .AsNoTracking()
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Create new gift
    /// Returns the generated GiftId
    /// </summary>
    public async Task<int> CreateGiftAsync(Gift gift)
    {
        gift.CreatedAt = DateTime.UtcNow;
        
        _context.Gifts.Add(gift);
        await _context.SaveChangesAsync();
        
        return gift.GiftId;
    }

    /// <summary>
    /// Update existing gift
    /// </summary>
    public async Task<bool> UpdateGiftAsync(Gift gift)
    {
        gift.UpdatedAt = DateTime.UtcNow;
        
        _context.Gifts.Update(gift);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Delete gift by ID
    /// </summary>
    public async Task<bool> DeleteGiftAsync(int giftId)
    {
        var gift = await _context.Gifts.FindAsync(giftId);
        if (gift == null)
            return false;

        _context.Gifts.Remove(gift);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Get all gifts from a specific donor
    /// </summary>
    public async Task<List<Gift>> GetGiftsByDonorAsync(int donorId)
    {
        return await _context.Gifts
            .AsNoTracking()
            .Where(g => g.DonorId == donorId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get all gifts in a specific category
    /// </summary>
    public async Task<List<Gift>> GetGiftsByCategoryAsync(int categoryId)
    {
        return await _context.Gifts
            .AsNoTracking()
            .Where(g => g.CategoryId == categoryId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get gifts within a price range
    /// </summary>
    public async Task<List<Gift>> GetGiftsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        return await _context.Gifts
            .AsNoTracking()
            .Where(g => g.Price >= minPrice && g.Price <= maxPrice)
            .OrderBy(g => g.Price)
            .ToListAsync();
    }

    /// <summary>
    /// Search gifts by name or description
    /// </summary>
    public async Task<List<Gift>> SearchGiftsAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();
        
        return await _context.Gifts
            .AsNoTracking()
            .Where(g => g.Name.ToLower().Contains(term) || 
                        g.Description.ToLower().Contains(term))
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get gifts with multiple filter criteria
    /// All filters are optional (null values are ignored)
    /// </summary>
    public async Task<List<Gift>> GetFilteredGiftsAsync(int? categoryId, int? donorId, decimal? minPrice, decimal? maxPrice)
    {
        var query = _context.Gifts.AsNoTracking();

        if (categoryId.HasValue)
            query = query.Where(g => g.CategoryId == categoryId.Value);

        if (donorId.HasValue)
            query = query.Where(g => g.DonorId == donorId.Value);

        if (minPrice.HasValue)
            query = query.Where(g => g.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(g => g.Price <= maxPrice.Value);

        return await query
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Update gift quantity (used when processing orders)
    /// quantityChange can be positive (restock) or negative (sale)
    /// </summary>
    public async Task<bool> UpdateGiftQuantityAsync(int giftId, int quantityChange)
    {
        var gift = await _context.Gifts.FindAsync(giftId);
        if (gift == null)
            return false;

        gift.Quantity += quantityChange;
        gift.UpdatedAt = DateTime.UtcNow;

        _context.Gifts.Update(gift);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }
}
