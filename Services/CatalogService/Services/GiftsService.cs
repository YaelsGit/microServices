using SharedModels.Models;
using SharedModels.Dtos;
using CatalogService.Interfaces;
using CatalogService.Services.Cache;

namespace CatalogService.Services;

public class GiftsService : IGiftsService
{
    private readonly IGiftsRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<GiftsService> _logger;

    private const string AllGiftsCacheKey = "gifts:all";
    private static string GiftCacheKey(int id) => $"gifts:{id}";

    public GiftsService(IGiftsRepository repository, ICacheService cache, ILogger<GiftsService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<DtoGifts>> GetAllGiftsAsync()
    {
        var cached = await _cache.GetAsync<List<DtoGifts>>(AllGiftsCacheKey);
        if (cached != null)
        {
            _logger.LogInformation("[CACHE HIT] Key: {Key} — returning {Count} gifts from Redis", AllGiftsCacheKey, cached.Count);
            return cached;
        }

        _logger.LogInformation("[CACHE MISS] Key: {Key} — fetching from MongoDB", AllGiftsCacheKey);
        var gifts = await _repository.GetAllGiftsAsync();
        var result = gifts.Select(MapToDto).ToList();

        await _cache.SetAsync(AllGiftsCacheKey, result);
        return result;
    }

    public async Task<DtoGifts?> GetGiftByIdAsync(int giftId)
    {
        if (giftId <= 0) return null;

        var key = GiftCacheKey(giftId);
        var cached = await _cache.GetAsync<DtoGifts>(key);
        if (cached != null)
        {
            _logger.LogInformation("[CACHE HIT] Key: {Key} — returning gift from Redis", key);
            return cached;
        }

        _logger.LogInformation("[CACHE MISS] Key: {Key} — fetching from MongoDB", key);
        var gift = await _repository.GetGiftByIdAsync(giftId);
        if (gift == null) return null;

        var dto = MapToDto(gift);
        await _cache.SetAsync(key, dto);
        return dto;
    }

    public async Task<int> CreateGiftAsync(DtoCreateGiftRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Quantity < 0 || request.Price <= 0)
            throw new ArgumentException("Invalid gift data");

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

        // Invalidate the all-gifts cache so next read fetches fresh data
        await _cache.RemoveAsync(AllGiftsCacheKey);
        _logger.LogInformation("[CACHE INVALIDATE] Key: {Key} — new gift {GiftId} created", AllGiftsCacheKey, giftId);

        return giftId;
    }

    public async Task<bool> UpdateGiftAsync(int giftId, DtoUpdateGiftRequest request)
    {
        var gift = await _repository.GetGiftByIdAsync(giftId);
        if (gift == null) return false;

        if (!string.IsNullOrWhiteSpace(request.Name)) gift.Name = request.Name;
        if (!string.IsNullOrWhiteSpace(request.Description)) gift.Description = request.Description;
        if (request.Price.HasValue && request.Price > 0) gift.Price = request.Price.Value;
        if (request.Quantity.HasValue && request.Quantity >= 0) gift.Quantity = request.Quantity.Value;
        if (request.DonorId.HasValue && request.DonorId > 0) gift.DonorId = request.DonorId.Value;
        if (request.CategoryId.HasValue && request.CategoryId > 0) gift.CategoryId = request.CategoryId.Value;

        var success = await _repository.UpdateGiftAsync(gift);
        if (success)
        {
            // Invalidate both the individual and list caches
            await Task.WhenAll(
                _cache.RemoveAsync(GiftCacheKey(giftId)),
                _cache.RemoveAsync(AllGiftsCacheKey));
            _logger.LogInformation("[CACHE INVALIDATE] Keys: {Key1}, {Key2} — gift {GiftId} updated",
                GiftCacheKey(giftId), AllGiftsCacheKey, giftId);
        }
        return success;
    }

    public async Task<bool> DeleteGiftAsync(int giftId)
    {
        var success = await _repository.DeleteGiftAsync(giftId);
        if (success)
        {
            await Task.WhenAll(
                _cache.RemoveAsync(GiftCacheKey(giftId)),
                _cache.RemoveAsync(AllGiftsCacheKey));
            _logger.LogInformation("[CACHE INVALIDATE] Keys: {Key1}, {Key2} — gift {GiftId} deleted",
                GiftCacheKey(giftId), AllGiftsCacheKey, giftId);
        }
        return success;
    }

    public async Task<bool> UpdateGiftQuantityAsync(int giftId, int quantityChange)
    {
        var gift = await _repository.GetGiftByIdAsync(giftId);
        if (gift == null) return false;

        var success = await _repository.UpdateGiftQuantityAsync(giftId, quantityChange);
        if (success)
        {
            await Task.WhenAll(
                _cache.RemoveAsync(GiftCacheKey(giftId)),
                _cache.RemoveAsync(AllGiftsCacheKey));
            _logger.LogInformation("[CACHE INVALIDATE] Keys: {Key1}, {Key2} — gift {GiftId} quantity changed by {Delta}",
                GiftCacheKey(giftId), AllGiftsCacheKey, giftId, quantityChange);
        }
        return success;
    }

    public async Task<IEnumerable<DtoGifts>> GetGiftsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
            return Enumerable.Empty<DtoGifts>();

        // Price-range queries are not cached (too many combinations) — go straight to MongoDB
        _logger.LogInformation("[CACHE SKIP] Price-range query ({Min}-{Max}) — fetching from MongoDB", minPrice, maxPrice);
        var gifts = await _repository.GetGiftsByPriceRangeAsync(minPrice, maxPrice);
        return gifts.Select(MapToDto);
    }

    public async Task<IEnumerable<DtoGifts>> GetFilteredGiftsAsync(DtoGiftFilter filter)
    {
        // Filtered queries are not cached — go straight to MongoDB
        _logger.LogInformation("[CACHE SKIP] Filtered query — fetching from MongoDB");
        var gifts = await _repository.GetFilteredGiftsAsync(
            filter.CategoryId, filter.DonorId, filter.MinPrice, filter.MaxPrice);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            gifts = gifts.Where(g =>
                g.Name.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                g.Description.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return gifts.Select(MapToDto);
    }

    private static DtoGifts MapToDto(Gift gift) => new()
    {
        GiftId = gift.GiftId,
        Name = gift.Name,
        Description = gift.Description,
        Price = gift.Price,
        Quantity = gift.Quantity,
        DonorId = gift.DonorId,
        CategoryId = gift.CategoryId,
        DonorName = string.Empty,
        CategoryName = string.Empty,
        CreatedAt = gift.CreatedAt,
        UpdatedAt = gift.UpdatedAt
    };
}
