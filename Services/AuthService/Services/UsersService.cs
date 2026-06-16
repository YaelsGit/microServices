using SharedModels.Interfaces;
using SharedModels.Models;
using SharedModels.Utilities;
using SharedModels.Dtos;
using AuthService.Interfaces;
using BCrypt.Net;

namespace AuthService.Services;

/// <summary>
/// Service implementation for user authentication and authorization
/// Implements IAuthService interface from SharedModels
/// Handles password hashing, JWT generation, and authentication logic
/// </summary>
public class UsersService : IAuthService
{
    private readonly IUsersRepository _repository;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<UsersService> _logger;

    public UsersService(
        IUsersRepository repository,
        JwtTokenService jwtTokenService,
        ILogger<UsersService> logger)
    {
        _repository = repository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <summary>
    /// User login - validates email and password, returns JWT token
    /// </summary>
    public async Task<(bool Success, string Token, int UserId, string Message)> LoginAsync(string email, string password)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Login attempt with empty email or password");
                return (false, string.Empty, 0, SharedModels.Constants.ErrorCodes.GetErrorMessage(
                    SharedModels.Constants.ErrorCodes.INVALID_CREDENTIALS));
            }

            // Retrieve user from database
            var user = await _repository.GetUserByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning($"Login attempt for non-existent user: {email}");
                return (false, string.Empty, 0, SharedModels.Constants.ErrorCodes.GetErrorMessage(
                    SharedModels.Constants.ErrorCodes.INVALID_CREDENTIALS));
            }

            // Verify password using BCrypt
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning($"Failed login attempt for user: {email}");
                return (false, string.Empty, 0, SharedModels.Constants.ErrorCodes.GetErrorMessage(
                    SharedModels.Constants.ErrorCodes.INVALID_CREDENTIALS));
            }

            // Generate JWT token
            var token = _jwtTokenService.GenerateToken(user.UserId, user.Email, user.FirstName, user.LastName, user.Role);
            
            _logger.LogInformation($"User successfully logged in: {email}");
            return (true, token, user.UserId, "Login successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return (false, string.Empty, 0, "An error occurred during login");
        }
    }

    /// <summary>
    /// User registration - creates new user with hashed password
    /// </summary>
    public async Task<(bool Success, int UserId, string Message)> RegisterAsync(
        string firstName, string lastName, string email, string password, string phoneNumber)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return (false, 0, "Email and password are required");
            }

            // Validate email format
            if (!ValidationHelpers.IsValidEmail(email))
            {
                _logger.LogWarning($"Registration attempt with invalid email: {email}");
                return (false, 0, SharedModels.Constants.ErrorCodes.GetErrorMessage(
                    SharedModels.Constants.ErrorCodes.INVALID_EMAIL));
            }

            // Validate password strength
            if (!ValidationHelpers.IsValidPassword(password))
            {
                _logger.LogWarning($"Registration attempt with weak password for email: {email}");
                return (false, 0, SharedModels.Constants.ErrorCodes.GetErrorMessage(
                    SharedModels.Constants.ErrorCodes.WEAK_PASSWORD));
            }

            // Check if user already exists
            var existingUser = await _repository.UserExistsByEmailAsync(email);
            if (existingUser)
            {
                _logger.LogWarning($"Registration attempt with existing email: {email}");
                return (false, 0, SharedModels.Constants.ErrorCodes.GetErrorMessage(
                    SharedModels.Constants.ErrorCodes.EMAIL_ALREADY_EXISTS));
            }

            // Hash password using BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password,12);

            // Create new user
            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = passwordHash,
                PhoneNumber = phoneNumber,
                Role = "user",
                CreatedAt = DateTime.UtcNow
            };

            var userId = await _repository.CreateUserAsync(user);
            _logger.LogInformation($"New user registered successfully: {email}");
            
            return (true, userId, "Registration successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return (false, 0, "An error occurred during registration");
        }
    }

    /// <summary>
    /// Validate JWT token and return user information
    /// </summary>
    public async Task<(bool Success, int UserId, string Message)> ValidateTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, 0, SharedModels.Constants.ErrorCodes.GetErrorMessage(
                    SharedModels.Constants.ErrorCodes.INVALID_TOKEN));
            }

            var (isValid, userId, email, role) = _jwtTokenService.ValidateToken(token);
            
            if (!isValid)
            {
                _logger.LogWarning("Invalid token validation attempt");
                return (false, 0, SharedModels.Constants.ErrorCodes.GetErrorMessage(
                    SharedModels.Constants.ErrorCodes.INVALID_TOKEN));
            }

            // Verify user still exists in database
            var user = await _repository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"Token validation for non-existent user: {userId}");
                return (false, 0, SharedModels.Constants.ErrorCodes.GetErrorMessage(
                    SharedModels.Constants.ErrorCodes.USER_NOT_FOUND));
            }

            _logger.LogInformation($"Token validated for user: {userId}");
            return (true, userId, "Token is valid");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return (false, 0, SharedModels.Constants.ErrorCodes.GetErrorMessage(
                SharedModels.Constants.ErrorCodes.INVALID_TOKEN));
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                _logger.LogWarning($"Password change attempt with empty values for user: {userId}");
                return false;
            }

            // Validate new password strength
            if (!ValidationHelpers.IsValidPassword(newPassword))
            {
                _logger.LogWarning($"Password change attempt with weak password for user: {userId}");
                return false;
            }

            // Get user
            var user = await _repository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"Password change attempt for non-existent user: {userId}");
                return false;
            }

            // Verify old password
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            {
                _logger.LogWarning($"Password change with incorrect old password for user: {userId}");
                return false;
            }

            // Hash new password and update
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
            var success = await _repository.UpdateUserAsync(user);

            if (success)
            {
                _logger.LogInformation($"Password changed successfully for user: {userId}");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error changing password for user: {userId}");
            return false;
        }
    }
}
