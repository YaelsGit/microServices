namespace SharedModels.Models;

public class Order
{
    public int OrderId { get; set; }
    public int UserId { get; set; } // FK - shadow property only, no navigation
    public int GiftId { get; set; } // FK - shadow property only, no navigation
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "pending"; // pending, completed, cancelled
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
