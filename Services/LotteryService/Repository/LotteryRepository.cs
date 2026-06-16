using Microsoft.EntityFrameworkCore;
using SharedModels.Models;
using LotteryService.Data;
using LotteryService.Interfaces;

namespace LotteryService.Repository;

/// <summary>
/// Repository implementation for Lottery data access
/// PURE data access layer: handles ONLY database operations
/// Business logic (lottery draws, winner selection, status transitions)
/// is implemented in LotteryService - NOT here
/// Note: No direct joins to User/Gift tables - only stores UserId and GiftId
/// </summary>
public class LotteryRepository : ILotteryRepository
{
    private readonly LotteryDbContext _context;

    public LotteryRepository(LotteryDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get lottery entry by ID
    /// </summary>
    public async Task<Lottery?> GetLotteryByIdAsync(int lotteryId)
    {
        return await _context.Lotteries
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LotteryId == lotteryId);
    }

    /// <summary>
    /// Get all lottery entries for a specific user
    /// </summary>
    public async Task<List<Lottery>> GetLotteryByUserIdAsync(int userId)
    {
        return await _context.Lotteries
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get all lottery entries
    /// </summary>
    public async Task<List<Lottery>> GetAllLotteriesAsync()
    {
        return await _context.Lotteries
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Create new lottery entry
    /// Returns the generated LotteryId
    /// Note: Status initialization is handled by the Service layer, not here
    /// </summary>
    public async Task<int> CreateLotteryAsync(Lottery lottery)
    {
        lottery.CreatedAt = DateTime.UtcNow;
        
        _context.Lotteries.Add(lottery);
        await _context.SaveChangesAsync();
        
        return lottery.LotteryId;
    }

    /// <summary>
    /// Update existing lottery entry
    /// </summary>
    public async Task<bool> UpdateLotteryAsync(Lottery lottery)
    {
        _context.Lotteries.Update(lottery);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Delete lottery entry by ID
    /// </summary>
    public async Task<bool> DeleteLotteryAsync(int lotteryId)
    {
        var lottery = await _context.Lotteries.FindAsync(lotteryId);
        if (lottery == null)
            return false;

        _context.Lotteries.Remove(lottery);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Get all lottery winners (status = "won")
    /// </summary>
    public async Task<List<Lottery>> GetWinnersAsync()
    {
        return await _context.Lotteries
            .AsNoTracking()
            .Where(l => l.Status == "won" && l.WonAt.HasValue)
            .OrderByDescending(l => l.WonAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get all lottery entries for a specific gift
    /// </summary>
    public async Task<List<Lottery>> GetLotteryByGiftIdAsync(int giftId)
    {
        return await _context.Lotteries
            .AsNoTracking()
            .Where(l => l.GiftId == giftId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get lottery entries by status (active, won, lost)
    /// </summary>
    public async Task<List<Lottery>> GetLotteryByStatusAsync(string status)
    {
        return await _context.Lotteries
            .AsNoTracking()
            .Where(l => l.Status == status)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Update lottery status
    /// Note: Business logic (e.g., when to set WonAt timestamp) is handled by Service layer
    /// Repository just updates the provided fields
    /// </summary>
    public async Task<bool> UpdateLotteryStatusAsync(int lotteryId, string newStatus)
    {
        var lottery = await _context.Lotteries.FindAsync(lotteryId);
        if (lottery == null)
            return false;

        lottery.Status = newStatus;

        _context.Lotteries.Update(lottery);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Mark lottery as won with timestamp
    /// Repository just sets the provided fields, business logic is in Service layer
    /// </summary>
    public async Task<bool> MarkAsWonAsync(int lotteryId)
    {
        var lottery = await _context.Lotteries.FindAsync(lotteryId);
        if (lottery == null)
            return false;

        lottery.WonAt = DateTime.UtcNow;

        _context.Lotteries.Update(lottery);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Get total number of lottery tickets
    /// </summary>
    public async Task<int> GetTotalTicketsAsync()
    {
        return await _context.Lotteries.CountAsync();
    }

    /// <summary>
    /// Get count of winning tickets
    /// </summary>
    public async Task<int> GetWinningTicketsCountAsync()
    {
        return await _context.Lotteries
            .CountAsync(l => l.Status == "won");
    }
}
