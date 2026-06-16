namespace LotteryService.HttpClients;

/// <summary>
/// HTTP client wrapper for CatalogService calls
/// Handles communication with CatalogService for gift retrieval and pricing
/// </summary>
public class CatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogServiceClient> _logger;

    public CatalogServiceClient(HttpClient httpClient, ILogger<CatalogServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Get gift details from CatalogService
    /// </summary>
    public async Task<(bool Success, int GiftId, string Name, decimal Price)> GetGiftAsync(int giftId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/catalog/gifts/{giftId}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Retrieved gift from CatalogService: {giftId}");
                // In a real scenario, deserialize the response
                return (true, giftId, "Gift Name", 0m);
            }

            _logger.LogWarning($"Gift not found in CatalogService: {giftId}");
            return (false, 0, string.Empty, 0m);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting gift from CatalogService: {giftId}");
            return (false, 0, string.Empty, 0m);
        }
    }

    /// <summary>
    /// Get all gifts from CatalogService
    /// Used for lottery draw operations
    /// </summary>
    public async Task<List<(int GiftId, string Name, decimal Price)>> GetAllGiftsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/catalog/gifts");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Retrieved all gifts from CatalogService");
                // In a real scenario, deserialize the response
                return new List<(int, string, decimal)>();
            }

            _logger.LogWarning("Failed to retrieve gifts from CatalogService");
            return new List<(int, string, decimal)>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all gifts from CatalogService");
            return new List<(int, string, decimal)>();
        }
    }

    /// <summary>
    /// Get gifts with available inventory for lottery
    /// </summary>
    public async Task<List<(int GiftId, string Name, decimal Price)>> GetAvailableGiftsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/catalog/gifts?filterByStock=true");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Retrieved available gifts from CatalogService");
                return new List<(int, string, decimal)>();
            }

            _logger.LogWarning("Failed to retrieve available gifts from CatalogService");
            return new List<(int, string, decimal)>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available gifts from CatalogService");
            return new List<(int, string, decimal)>();
        }
    }
}
