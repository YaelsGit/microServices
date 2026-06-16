namespace SharedModels.Interfaces;

public interface ILotteryService
{
    Task<(bool Success, int WinningTicketCount, string Message)> RunLotteryAsync();
    Task<IEnumerable<SharedModels.Dtos.DtoLotteryWinner>> GetWinnersAsync();
    Task<IEnumerable<SharedModels.Dtos.DtoLottery>> GetUserLotteryTicketsAsync(int userId);
    Task<SharedModels.Dtos.DtoLotteryStatistics> GetLotteryStatisticsAsync();
    Task<bool> CreateLotteryTicketAsync(int userId, int giftId);
}
