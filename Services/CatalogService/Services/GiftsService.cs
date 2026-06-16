using SharedModels.Models;
using SharedModels.Dtos;
using SharedModels.Constants;
using CatalogService.Interfaces;

namespace CatalogService.Services;

/// <summary>
/// Service implementation for Gift business logic
/// Handles CRUD operations, filtering, and gift-related business rules
/// </summary>
public class GiftsService : IGiftsService
{
    private readonly IGiftsRepository _repository;
    private readonly ILogger<GiftsService> _logger;

    public GiftsService(IGiftsRepository repository, ILogger<GiftsService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Get all gifts
    /// </summary>
    public async Task<IEnumerable<DtoGifts>> GetAllGiftsAsync()
    {
        try
        {
            var gifts = await _repository.GetAllGiftsAsync();
            return gifts.Select(g => MapToDto(g));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all gifts");
            throw;
        }
    }

    /// <summary>
    /// Get gift by ID
    /// </summary>
    public async Task<DtoGifts?> GetGiftByIdAsync(int giftId)
    {
        try
        {
            if (giftId <= 0)
                return null;

            var gift = await _repository.GetGiftByIdAsync(giftId);
            return gift == null ? null : MapToDto(gift);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting gift: {giftId}");
            throw;
        }
    }

    /// <summary>
    /// Create new gift
    /// </summary>
    public async Task<int> CreateGiftAsync(DtoCreateGiftRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Name) || request.Quantity < 0 || request.Price <= 0)
            {
                _logger.LogWarning("Create gift attempt with invalid data");
                throw new ArgumentException("Invalid gift data");
            }

            var gift = new Gift
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Quantity = request.Quantity,
                DonorId = request.DonorId,
                CategoryId = request.CategoryId
            };

            var giftId = await _repository.CreateGiftAsync(gift);
            _logger.LogInformation($"New gift created: {giftId}");
            return giftId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gift");
            throw;
        }
    }

    /// <summary>
    /// Update gift
    /// </summary>
    public async Task<bool> UpdateGiftAsync(int giftId, DtoUpdateGiftRequest request)
    {
        try
        {
            var gift = await _repository.GetGiftByIdAsync(giftId);
            if (gift == null)
            {
                _logger.LogWarning($"Update gift attempt for non-existent gift: {giftId}");
                return false;
            }

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(request.Name))
                gift.Name = request.Name;
            if (!string.IsNullOrWhiteSpace(request.Description))
                gift.Description = request.Description;
            if (request.Price.HasValue && request.Price > 0)
                gift.Price = request.Price.Value;
            if (request.Quantity.HasValue && request.Quantity >= 0)
                gift.Quantity = request.Quantity.Value;
            if (request.DonorId.HasValue && request.DonorId > 0)
                gift.DonorId = request.DonorId.Value;
            if (request.CategoryId.HasValue && request.CategoryId > 0)
                gift.CategoryId = request.CategoryId.Value;

            var success = await _repository.UpdateGiftAsync(gift);
            if (success)
            {
                _logger.LogInformation($"Gift updated: {giftId}");
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating gift: {giftId}");
            throw;
        }
    }

    /// <summary>
    /// Delete gift
    /// </summary>
    public async Task<bool> DeleteGiftAsync(int giftId)
    {
        try
        {
            var success = await _repository.DeleteGiftAsync(giftId);
            if (success)
            {
                _logger.LogInformation($"Gift deleted: {giftId}");
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting gift: {giftId}");
            throw;
        }
    }

    /// <summary>
    /// Get gifts by price range
    /// </summary>
    public async Task<IEnumerable<DtoGifts>> GetGiftsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        try
        {
            // Validate price range
            if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
            {
                _logger.LogWarning("Invalid price range requested");
                return Enumerable.Empty<DtoGifts>();
            }

            var gifts = await _repository.GetGiftsByPriceRangeAsync(minPrice, maxPrice);
            return gifts.Select(g => MapToDto(g));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gifts by price range");
            throw;
        }
    }

    /// <summary>
    /// Get filtered gifts
    /// </summary>
    public async Task<IEnumerable<DtoGifts>> GetFilteredGiftsAsync(DtoGiftFilter filter)
    {
        try
        {
            var gifts = await _repository.GetFilteredGiftsAsync(
                filter.CategoryId, 
                filter.DonorId, 
                filter.MinPrice, 
                filter.MaxPrice);

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                gifts = gifts
                    .Where(g => g.Name.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                               g.Description.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return gifts.Select(g => MapToDto(g));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered gifts");
            throw;
        }
    }

    /// <summary>
    /// Helper method to map Gift to DtoGifts
    /// </summary>
    private DtoGifts MapToDto(Gift gift)
    {
        return new DtoGifts
        {
            GiftId = gift.GiftId,
            Name = gift.Name,
            Description = gift.Description,
            Price = gift.Price,
            Quantity = gift.Quantity,
            DonorId = gift.DonorId,
            CategoryId = gift.CategoryId,
            DonorName = string.Empty, // Would be populated from CatalogService
            CategoryName = string.Empty, // Would be populated from CatalogService
            CreatedAt = gift.CreatedAt,
            UpdatedAt = gift.UpdatedAt
        };
    }
}
