namespace SharedModels.Dtos;

public class DtoLottery
{
    public int LotteryId { get; set; }
    public int UserId { get; set; }
    public int GiftId { get; set; }
    public string Status { get; set; } = "active"; // active, won, lost
    public string UserEmail { get; set; } = string.Empty;
    public string GiftName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? WonAt { get; set; }
}

public class DtoLotteryTicket
{
    public int TicketId { get; set; }
    public int GiftId { get; set; }
    public string GiftName { get; set; } = string.Empty;
    public decimal GiftPrice { get; set; }
}

public class DtoLotteryWinner
{
    public int LotteryId { get; set; }
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int GiftId { get; set; }
    public string GiftName { get; set; } = string.Empty;
    public DateTime WonAt { get; set; }
}

public class DtoLotteryStatistics
{
    public int TotalTickets { get; set; }
    public int WinningTickets { get; set; }
    public int LosingTickets { get; set; }
    public decimal TotalGiftValue { get; set; }
    public DateTime LastLotteryRun { get; set; }
}
