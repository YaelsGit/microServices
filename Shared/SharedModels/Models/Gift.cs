namespace SharedModels.Models;

public class Gift
{
    public int GiftId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int DonorId { get; set; } // FK - no navigation property to prevent cross-service joins
    public int CategoryId { get; set; } // FK - no navigation property
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
