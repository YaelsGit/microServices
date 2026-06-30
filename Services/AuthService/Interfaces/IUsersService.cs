namespace AuthService.Interfaces;

/// <summary>
/// Service interface for user authentication operations
/// Handles user login, registration, token validation, and password management
/// </summary>
public interface IUsersService
{
    Task<(bool Success, string Token, int UserId, string Message)> LoginAsync(string email, string password);
    Task<(bool Success, int UserId, string Message)> RegisterAsync(string firstName, string lastName, string email, string password, string phoneNumber);
    Task<(bool Success, int UserId, string Message)> ValidateTokenAsync(string token);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
}
