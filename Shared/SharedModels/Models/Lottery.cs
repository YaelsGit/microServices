namespace SharedModels.Models;

public class Lottery
{
    public int LotteryId { get; set; }
    public int UserId { get; set; } // FK - shadow property only, no navigation
    public int GiftId { get; set; } // FK - shadow property only, no navigation
    public string Status { get; set; } = "active"; // active, won, lost
    public DateTime CreatedAt { get; set; }
    public DateTime? WonAt { get; set; }
}
