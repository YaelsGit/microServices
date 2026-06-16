using SharedModels.Models;
using SharedModels.Dtos;
using SharedModels.Constants;
using LotteryService.Interfaces;
using LotteryService.HttpClients;

namespace LotteryService.Services;

/// <summary>
/// Service implementation for Lottery business logic
/// Handles lottery operations, draws, and inter-service communication
/// Depends on: LotteryRepository, AuthServiceClient, CatalogServiceClient
/// </summary>
public class LotteryDrawService : ILotteryService
{
    private readonly ILotteryRepository _repository;
    private readonly AuthServiceClient _authServiceClient;
    private readonly CatalogServiceClient _catalogServiceClient;
    private readonly ILogger<LotteryDrawService> _logger;
    private readonly Random _random = new Random();

    public LotteryDrawService(
        ILotteryRepository repository,
        AuthServiceClient authServiceClient,
        CatalogServiceClient catalogServiceClient,
        ILogger<LotteryDrawService> logger)
    {
        _repository = repository;
        _authServiceClient = authServiceClient;
        _catalogServiceClient = catalogServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Create lottery ticket for a user
    /// Business logic: Validate user and gift exist
    /// </summary>
    public async Task<bool> CreateLotteryTicketAsync(int userId, int giftId)
    {
        try
        {
            _logger.LogInformation($"Creating lottery ticket for user: {userId}, gift: {giftId}");

            // Validate user exists
            var userExists = await _authServiceClient.UserExistsAsync(userId);
            if (!userExists)
            {
                _logger.LogWarning($"Lottery ticket creation for non-existent user: {userId}");
                return false;
            }

            // Validate gift exists
            var (giftFound, _, _, _) = await _catalogServiceClient.GetGiftAsync(giftId);
            if (!giftFound)
            {
                _logger.LogWarning($"Lottery ticket creation for non-existent gift: {giftId}");
                return false;
            }

            // Create lottery entry
            var lottery = new Lottery
            {
                UserId = userId,
                GiftId = giftId,
                Status = "active"
            };

            var lotteryId = await _repository.CreateLotteryAsync(lottery);
            _logger.LogInformation($"Lottery ticket created: {lotteryId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating lottery ticket for user: {userId}");
            return false;
        }
    }

    /// <summary>
    /// Run lottery draw - select random winners from active tickets
    /// Business logic:
    /// 1. Get all active lottery entries
    /// 2. Randomly select one winner per gift
    /// 3. Update status to "won" and set WonAt timestamp
    /// </summary>
    public async Task<(bool Success, int WinningTicketCount, string Message)> RunLotteryAsync()
    {
        try
        {
            _logger.LogInformation("Starting lottery draw");

            // Get all active lottery entries
            var activeTickets = await _repository.GetLotteryByStatusAsync("active");
            if (activeTickets.Count == 0)
            {
                _logger.LogWarning("No active lottery tickets for draw");
                return (false, 0, "No active tickets");
            }

            // Group by gift to select one winner per gift
            var ticketsByGift = activeTickets.GroupBy(t => t.GiftId);
            int winnerCount = 0;

            foreach (var giftGroup in ticketsByGift)
            {
                // Randomly select winner for this gift
                int winnerIndex = _random.Next(giftGroup.Count());
                var winner = giftGroup.ElementAt(winnerIndex);

                // Update status to won
                var success = await _repository.UpdateLotteryStatusAsync(winner.LotteryId, "won");
                if (success)
                {
                    // Mark as won (sets WonAt timestamp)
                    await _repository.MarkAsWonAsync(winner.LotteryId);
                    winnerCount++;

                    _logger.LogInformation($"Lottery winner selected: {winner.LotteryId} for gift: {giftGroup.Key}");
                }
            }

            // Mark all non-winning tickets as lost
            var losingTickets = activeTickets
                .Where(t => !ticketsByGift.Any(g => g.Any(ticket => ticket.LotteryId == t.LotteryId && 
                    t.GiftId == g.Key && _random.Next(2) == 0)))
                .ToList();

            foreach (var ticket in losingTickets)
            {
                await _repository.UpdateLotteryStatusAsync(ticket.LotteryId, "lost");
            }

            _logger.LogInformation($"Lottery draw completed with {winnerCount} winners");
            return (true, winnerCount, "Lottery draw completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running lottery draw");
            return (false, 0, "Error during lottery draw");
        }
    }

    /// <summary>
    /// Get all lottery winners
    /// </summary>
    public async Task<IEnumerable<DtoLotteryWinner>> GetWinnersAsync()
    {
        try
        {
            var winners = await _repository.GetWinnersAsync();
            var result = new List<DtoLotteryWinner>();

            foreach (var winner in winners)
            {
                // Get user details from AuthService
                var (userFound, email, firstName, lastName) = await _authServiceClient.GetUserAsync(winner.UserId);
                
                // Get gift details from CatalogService
                var (giftFound, _, giftName, _) = await _catalogServiceClient.GetGiftAsync(winner.GiftId);

                result.Add(new DtoLotteryWinner
                {
                    LotteryId = winner.LotteryId,
                    UserId = winner.UserId,
                    UserEmail = email,
                    UserName = $"{firstName} {lastName}",
                    GiftId = winner.GiftId,
                    GiftName = giftName,
                    WonAt = winner.WonAt ?? DateTime.UtcNow
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lottery winners");
            throw;
        }
    }

    /// <summary>
    /// Get user's lottery tickets
    /// </summary>
    public async Task<IEnumerable<DtoLottery>> GetUserLotteryTicketsAsync(int userId)
    {
        try
        {
            var tickets = await _repository.GetLotteryByUserIdAsync(userId);
            return tickets.Select(t => MapToDto(t));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting lottery tickets for user: {userId}");
            throw;
        }
    }

    /// <summary>
    /// Get lottery statistics
    /// </summary>
    public async Task<DtoLotteryStatistics> GetLotteryStatisticsAsync()
    {
        try
        {
            var totalTickets = await _repository.GetTotalTicketsAsync();
            var winningTickets = await _repository.GetWinningTicketsCountAsync();
            var winners = await _repository.GetWinnersAsync();

            decimal totalGiftValue = 0;
            foreach (var winner in winners)
            {
                var (found, _, _, price) = await _catalogServiceClient.GetGiftAsync(winner.GiftId);
                if (found)
                    totalGiftValue += price;
            }

            return new DtoLotteryStatistics
            {
                TotalTickets = totalTickets,
                WinningTickets = winningTickets,
                LosingTickets = totalTickets - winningTickets,
                TotalGiftValue = totalGiftValue,
                LastLotteryRun = DateTime.UtcNow // In production, track this separately
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lottery statistics");
            throw;
        }
    }

    /// <summary>
    /// Helper method to map Lottery to DtoLottery
    /// </summary>
    private DtoLottery MapToDto(Lottery lottery)
    {
        return new DtoLottery
        {
            LotteryId = lottery.LotteryId,
            UserId = lottery.UserId,
            GiftId = lottery.GiftId,
            Status = lottery.Status,
            UserEmail = string.Empty, // Would be populated from AuthService
            GiftName = string.Empty, // Would be populated from CatalogService
            CreatedAt = lottery.CreatedAt,
            WonAt = lottery.WonAt
        };
    }
}
