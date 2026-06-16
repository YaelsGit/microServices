using SharedModels.Models;

namespace CatalogService.Interfaces;

/// <summary>
/// Repository interface for Gift data access in CatalogService
/// Includes filtering and search capabilities
/// </summary>
public interface IGiftsRepository
{
    Task<Gift?> GetGiftByIdAsync(int giftId);
    Task<List<Gift>> GetAllGiftsAsync();
    Task<int> CreateGiftAsync(Gift gift);
    Task<bool> UpdateGiftAsync(Gift gift);
    Task<bool> DeleteGiftAsync(int giftId);
    Task<List<Gift>> GetGiftsByDonorAsync(int donorId);
    Task<List<Gift>> GetGiftsByCategoryAsync(int categoryId);
    Task<List<Gift>> GetGiftsByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    Task<List<Gift>> SearchGiftsAsync(string searchTerm);
    Task<List<Gift>> GetFilteredGiftsAsync(int? categoryId, int? donorId, decimal? minPrice, decimal? maxPrice);
    Task<bool> UpdateGiftQuantityAsync(int giftId, int quantityChange);
}
