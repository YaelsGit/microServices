namespace LotteryService.HttpClients;

/// <summary>
/// HTTP client wrapper for AuthService calls
/// Handles communication with AuthService for user validation and notification
/// </summary>
public class AuthServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthServiceClient> _logger;

    public AuthServiceClient(HttpClient httpClient, ILogger<AuthServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Get user details from AuthService
    /// </summary>
    public async Task<(bool Success, string Email, string FirstName, string LastName)> GetUserAsync(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/auth/users/{userId}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Retrieved user from AuthService: {userId}");
                // In a real scenario, deserialize the response
                return (true, "user@example.com", "First", "Last");
            }

            _logger.LogWarning($"User not found in AuthService: {userId}");
            return (false, string.Empty, string.Empty, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user from AuthService: {userId}");
            return (false, string.Empty, string.Empty, string.Empty);
        }
    }

    /// <summary>
    /// Check if user exists in AuthService
    /// </summary>
    public async Task<bool> UserExistsAsync(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/auth/users/{userId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking user existence: {userId}");
            return false;
        }
    }
}
