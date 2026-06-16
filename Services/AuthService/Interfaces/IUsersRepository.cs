using SharedModels.Models;

namespace AuthService.Interfaces;

/// <summary>
/// Repository interface for User data access in AuthService
/// Defines ONLY data access operations
/// Authentication and password logic are handled by the Service layer
/// </summary>
public interface IUsersRepository
{
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<int> CreateUserAsync(User user);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(int userId);
    Task<bool> UserExistsByEmailAsync(string email);
    Task<List<User>> GetAllUsersAsync();
}
