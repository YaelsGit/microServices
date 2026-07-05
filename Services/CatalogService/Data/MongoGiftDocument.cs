using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CatalogService.Data;

/// <summary>
/// MongoDB document model for Gift.
/// Stored in the "gifts" collection in Mechira-CatalogService MongoDB database.
/// Uses document model to allow flexible attributes per gift category.
/// </summary>
public class MongoGiftDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("giftId")]
    public int GiftId { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("price")]
    public decimal Price { get; set; }

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    [BsonElement("donorId")]
    public int DonorId { get; set; }

    [BsonElement("categoryId")]
    public int CategoryId { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
