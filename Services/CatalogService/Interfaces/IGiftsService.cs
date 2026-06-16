using SharedModels.Dtos;

namespace CatalogService.Interfaces;

/// <summary>
/// Service interface for Gift business logic
/// Defines contracts for gift operations with filtering and search
/// </summary>
public interface IGiftsService
{
    Task<IEnumerable<DtoGifts>> GetAllGiftsAsync();
    Task<DtoGifts?> GetGiftByIdAsync(int giftId);
    Task<int> CreateGiftAsync(DtoCreateGiftRequest request);
    Task<bool> UpdateGiftAsync(int giftId, DtoUpdateGiftRequest request);
    Task<bool> DeleteGiftAsync(int giftId);
    Task<IEnumerable<DtoGifts>> GetGiftsByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    Task<IEnumerable<DtoGifts>> GetFilteredGiftsAsync(DtoGiftFilter filter);
}
