using SharedModels.Models;

namespace LotteryService.Interfaces;

/// <summary>
/// Repository interface for Lottery data access in LotteryService
/// </summary>
public interface ILotteryRepository
{
    Task<Lottery?> GetLotteryByIdAsync(int lotteryId);
    Task<List<Lottery>> GetLotteryByUserIdAsync(int userId);
    Task<List<Lottery>> GetAllLotteriesAsync();
    Task<int> CreateLotteryAsync(Lottery lottery);
    Task<bool> UpdateLotteryAsync(Lottery lottery);
    Task<bool> DeleteLotteryAsync(int lotteryId);
    
    // Lottery-specific queries
    Task<List<Lottery>> GetWinnersAsync();
    Task<List<Lottery>> GetLotteryByGiftIdAsync(int giftId);
    Task<List<Lottery>> GetLotteryByStatusAsync(string status);
    Task<bool> UpdateLotteryStatusAsync(int lotteryId, string newStatus);
    Task<bool> MarkAsWonAsync(int lotteryId);
    Task<int> GetTotalTicketsAsync();
    Task<int> GetWinningTicketsCountAsync();
}
