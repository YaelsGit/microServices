namespace OrderService.HttpClients;

/// <summary>
/// HTTP client wrapper for CatalogService calls
/// Handles communication with CatalogService for gift validation and inventory
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
    public async Task<(bool Success, decimal Price, int Quantity, string Name)> GetGiftAsync(int giftId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/catalog/gifts/{giftId}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Retrieved gift from CatalogService: {giftId}");
                // In a real scenario, deserialize the response
                return (true, 0m, 0, "Gift Name");
            }

            _logger.LogWarning($"Gift not found in CatalogService: {giftId}");
            return (false, 0m, 0, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting gift from CatalogService: {giftId}");
            return (false, 0m, 0, string.Empty);
        }
    }

    /// <summary>
    /// Check if gift exists and has inventory in CatalogService
    /// </summary>
    public async Task<bool> GiftExistsAndInStockAsync(int giftId, int quantity)
    {
        try
        {
            var (success, _, available, _) = await GetGiftAsync(giftId);
            if (!success)
                return false;

            return available >= quantity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking gift inventory: {giftId}");
            return false;
        }
    }

    /// <summary>
    /// Reduce gift quantity after order creation
    /// </summary>
    public async Task<bool> ReduceGiftQuantityAsync(int giftId, int quantity)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, $"/catalog/gifts/{giftId}/quantity");
            request.Content = new StringContent($"{{\"quantityChange\": -{quantity}}}", 
                System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Reduced gift quantity: {giftId} by {quantity}");
                return true;
            }

            _logger.LogWarning($"Failed to reduce gift quantity: {giftId}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reducing gift quantity: {giftId}");
            return false;
        }
    }
}
