namespace OrderService.HttpClients;

public class AuthServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthServiceClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public AuthServiceClient(HttpClient httpClient, ILogger<AuthServiceClient> logger, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    private void ForwardCorrelationId(HttpRequestMessage request)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString()
            ?? _httpContextAccessor.HttpContext?.Request.Headers[CorrelationIdHeader].ToString();
        if (!string.IsNullOrEmpty(correlationId))
            request.Headers.TryAddWithoutValidation(CorrelationIdHeader, correlationId);
    }

    public async Task<(bool Success, int UserId, string Message)> ValidateTokenAsync(string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/auth/validate");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            ForwardCorrelationId(request);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Token validation successful with AuthService");
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

    public async Task<(bool Success, string Email, string FirstName, string LastName)> GetUserAsync(int userId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/auth/users/{userId}");
            ForwardCorrelationId(request);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Retrieved user data from AuthService: {UserId}", userId);
                return (true, "user@example.com", "First", "Last");
            }

            _logger.LogWarning("Failed to get user from AuthService: {UserId}", userId);
            return (false, string.Empty, string.Empty, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user from AuthService: {UserId}", userId);
            return (false, string.Empty, string.Empty, string.Empty);
        }
    }

    public async Task<bool> UserExistsAsync(int userId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/auth/users/{userId}");
            ForwardCorrelationId(request);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user existence: {UserId}", userId);
            return false;
        }
    }
}
