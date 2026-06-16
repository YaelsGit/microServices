using Microsoft.EntityFrameworkCore;
using SharedModels.Models;
using AuthService.Data;
using AuthService.Interfaces;

namespace AuthService.Repository;

/// <summary>
/// Repository implementation for User data access
/// Strictly follows Repository pattern: handles ONLY database operations
/// Authentication and password logic are handled by the Service layer
/// </summary>
public class UsersRepository : IUsersRepository
{
    private readonly AuthDbContext _context;

    public UsersRepository(AuthDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    /// <summary>
    /// Get user by email address
    /// </summary>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Create new user
    /// Returns the generated UserId
    /// </summary>
    public async Task<int> CreateUserAsync(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return user.UserId;
    }

    /// <summary>
    /// Update existing user
    /// </summary>
    public async Task<bool> UpdateUserAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        
        _context.Users.Update(user);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Delete user by ID
    /// </summary>
    public async Task<bool> DeleteUserAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        _context.Users.Remove(user);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Check if user exists with given email
    /// </summary>
    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email);
    }

    /// <summary>
    /// Get all users (for admin purposes)
    /// </summary>
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }
}
