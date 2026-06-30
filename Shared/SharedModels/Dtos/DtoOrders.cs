namespace SharedModels.Dtos;

public class DtoOrders
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public int GiftId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "pending"; // pending, completed, cancelled
    public string UserEmail { get; set; } = string.Empty;
    public string GiftName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class DtoCreateOrderRequest
{
    public int UserId { get; set; }
    public int GiftId { get; set; }
    public int Quantity { get; set; }
}

public class DtoOrderReport
{
    public int GiftId { get; set; }
    public string GiftName { get; set; } = string.Empty;
    public int TotalOrdered { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class DtoOrderByPriceRange
{
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
}
