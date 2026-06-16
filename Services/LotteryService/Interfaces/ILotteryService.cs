using SharedModels.Dtos;

namespace LotteryService.Interfaces;

/// <summary>
/// Service interface for Lottery business logic
/// Defines contracts for lottery operations and inter-service communication
/// </summary>
public interface ILotteryService
{
    Task<bool> CreateLotteryTicketAsync(int userId, int giftId);
    Task<(bool Success, int WinningTicketCount, string Message)> RunLotteryAsync();
    Task<IEnumerable<DtoLotteryWinner>> GetWinnersAsync();
    Task<IEnumerable<DtoLottery>> GetUserLotteryTicketsAsync(int userId);
    Task<DtoLotteryStatistics> GetLotteryStatisticsAsync();
}
