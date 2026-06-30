namespace SharedModels.Dtos;

public class DtoGifts
{
    public int GiftId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int DonorId { get; set; }
    public int CategoryId { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class DtoCreateGiftRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int DonorId { get; set; }
    public int CategoryId { get; set; }
}

public class DtoUpdateGiftRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int? Quantity { get; set; }
    public int? DonorId { get; set; }
    public int? CategoryId { get; set; }
}

public class DtoGiftFilter
{
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? CategoryId { get; set; }
    public int? DonorId { get; set; }
    public string? SearchTerm { get; set; }
}

public class DtoUpdateGiftQuantityRequest
{
    public int QuantityChange { get; set; }
}
