using MongoDB.Driver;
using SharedModels.Models;
using CatalogService.Data;
using CatalogService.Interfaces;

namespace CatalogService.Repository;

/// <summary>
/// MongoDB repository for Gifts.
/// Replaces the SQL Server GiftsRepository — gifts are stored as documents
/// in MongoDB to support flexible per-category attributes (polyglot persistence).
/// </summary>
public class MongoGiftsRepository : IGiftsRepository
{
    private readonly IMongoCollection<MongoGiftDocument> _collection;
    private readonly ILogger<MongoGiftsRepository> _logger;

    public MongoGiftsRepository(IMongoDatabase database, ILogger<MongoGiftsRepository> logger)
    {
        _collection = database.GetCollection<MongoGiftDocument>("gifts");
        _logger = logger;

        // Ensure unique index on giftId
        var indexModel = new CreateIndexModel<MongoGiftDocument>(
            Builders<MongoGiftDocument>.IndexKeys.Ascending(g => g.GiftId),
            new CreateIndexOptions { Unique = true });
        _collection.Indexes.CreateOne(indexModel);
    }

    public async Task<Gift?> GetGiftByIdAsync(int giftId)
    {
        var doc = await _collection.Find(g => g.GiftId == giftId).FirstOrDefaultAsync();
        return doc == null ? null : MapToModel(doc);
    }

    public async Task<List<Gift>> GetAllGiftsAsync()
    {
        var docs = await _collection.Find(_ => true)
            .SortByDescending(g => g.CreatedAt)
            .ToListAsync();
        return docs.Select(MapToModel).ToList();
    }

    public async Task<int> CreateGiftAsync(Gift gift)
    {
        // Auto-increment: get max giftId + 1
        var maxDoc = await _collection.Find(_ => true)
            .SortByDescending(g => g.GiftId)
            .Limit(1)
            .FirstOrDefaultAsync();

        gift.GiftId = (maxDoc?.GiftId ?? 0) + 1;
        gift.CreatedAt = DateTime.UtcNow;

        var doc = MapToDocument(gift);
        await _collection.InsertOneAsync(doc);
        _logger.LogInformation("[MongoGiftsRepository] Gift created with GiftId: {GiftId}", gift.GiftId);
        return gift.GiftId;
    }

    public async Task<bool> UpdateGiftAsync(Gift gift)
    {
        gift.UpdatedAt = DateTime.UtcNow;
        var doc = MapToDocument(gift);
        var result = await _collection.ReplaceOneAsync(g => g.GiftId == gift.GiftId, doc);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteGiftAsync(int giftId)
    {
        var result = await _collection.DeleteOneAsync(g => g.GiftId == giftId);
        return result.DeletedCount > 0;
    }

    public async Task<List<Gift>> GetGiftsByDonorAsync(int donorId)
    {
        var docs = await _collection.Find(g => g.DonorId == donorId)
            .SortByDescending(g => g.CreatedAt)
            .ToListAsync();
        return docs.Select(MapToModel).ToList();
    }

    public async Task<List<Gift>> GetGiftsByCategoryAsync(int categoryId)
    {
        var docs = await _collection.Find(g => g.CategoryId == categoryId)
            .SortByDescending(g => g.CreatedAt)
            .ToListAsync();
        return docs.Select(MapToModel).ToList();
    }

    public async Task<List<Gift>> GetGiftsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        var filter = Builders<MongoGiftDocument>.Filter.And(
            Builders<MongoGiftDocument>.Filter.Gte(g => g.Price, minPrice),
            Builders<MongoGiftDocument>.Filter.Lte(g => g.Price, maxPrice));
        var docs = await _collection.Find(filter).SortBy(g => g.Price).ToListAsync();
        return docs.Select(MapToModel).ToList();
    }

    public async Task<List<Gift>> SearchGiftsAsync(string searchTerm)
    {
        var filter = Builders<MongoGiftDocument>.Filter.Or(
            Builders<MongoGiftDocument>.Filter.Regex(g => g.Name, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
            Builders<MongoGiftDocument>.Filter.Regex(g => g.Description, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")));
        var docs = await _collection.Find(filter).ToListAsync();
        return docs.Select(MapToModel).ToList();
    }

    public async Task<List<Gift>> GetFilteredGiftsAsync(int? categoryId, int? donorId, decimal? minPrice, decimal? maxPrice)
    {
        var filters = new List<FilterDefinition<MongoGiftDocument>>();

        if (categoryId.HasValue)
            filters.Add(Builders<MongoGiftDocument>.Filter.Eq(g => g.CategoryId, categoryId.Value));
        if (donorId.HasValue)
            filters.Add(Builders<MongoGiftDocument>.Filter.Eq(g => g.DonorId, donorId.Value));
        if (minPrice.HasValue)
            filters.Add(Builders<MongoGiftDocument>.Filter.Gte(g => g.Price, minPrice.Value));
        if (maxPrice.HasValue)
            filters.Add(Builders<MongoGiftDocument>.Filter.Lte(g => g.Price, maxPrice.Value));

        var combined = filters.Count > 0
            ? Builders<MongoGiftDocument>.Filter.And(filters)
            : Builders<MongoGiftDocument>.Filter.Empty;

        var docs = await _collection.Find(combined).SortByDescending(g => g.CreatedAt).ToListAsync();
        return docs.Select(MapToModel).ToList();
    }

    public async Task<bool> UpdateGiftQuantityAsync(int giftId, int quantityChange)
    {
        var update = Builders<MongoGiftDocument>.Update
            .Inc(g => g.Quantity, quantityChange)
            .Set(g => g.UpdatedAt, DateTime.UtcNow);
        var result = await _collection.UpdateOneAsync(g => g.GiftId == giftId, update);
        return result.ModifiedCount > 0;
    }

    private static Gift MapToModel(MongoGiftDocument doc) => new()
    {
        GiftId = doc.GiftId,
        Name = doc.Name,
        Description = doc.Description,
        Price = doc.Price,
        Quantity = doc.Quantity,
        DonorId = doc.DonorId,
        CategoryId = doc.CategoryId,
        CreatedAt = doc.CreatedAt,
        UpdatedAt = doc.UpdatedAt
    };

    private static MongoGiftDocument MapToDocument(Gift gift) => new()
    {
        GiftId = gift.GiftId,
        Name = gift.Name,
        Description = gift.Description,
        Price = gift.Price,
        Quantity = gift.Quantity,
        DonorId = gift.DonorId,
        CategoryId = gift.CategoryId,
        CreatedAt = gift.CreatedAt,
        UpdatedAt = gift.UpdatedAt
    };
}
