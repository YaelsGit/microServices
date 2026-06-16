namespace OrderService.HttpClients;

/// <summary>
/// HTTP client wrapper for AuthService calls
/// Handles communication with AuthService for user validation
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
    /// Validate user token with AuthService
    /// </summary>
    public async Task<(bool Success, int UserId, string Message)> ValidateTokenAsync(string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/auth/validate");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Token validation successful with AuthService");
                // In a real scenario, parse the response and extract UserId
                return (true, 0, "Token valid");
            }

            _logger.LogWarning("Token validation failed with AuthService");
            return (false, 0, "Invalid token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token with AuthService");
            return (false, 0, "Service error");
        }
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
                _logger.LogInformation($"Retrieved user data from AuthService: {userId}");
                // In a real scenario, deserialize the response
                return (true, "user@example.com", "First", "Last");
            }

            _logger.LogWarning($"Failed to get user from AuthService: {userId}");
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
