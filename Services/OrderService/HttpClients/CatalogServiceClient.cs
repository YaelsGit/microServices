namespace OrderService.HttpClients;

public class CatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogServiceClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CatalogServiceClient(HttpClient httpClient, ILogger<CatalogServiceClient> logger, IHttpContextAccessor httpContextAccessor)
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

    public async Task<(bool Success, decimal Price, int Quantity, string Name)> GetGiftAsync(int giftId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/catalog/gifts/{giftId}");
            ForwardCorrelationId(request);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Retrieved gift from CatalogService: {GiftId}", giftId);
                return (true, 0m, 0, "Gift Name");
            }

            _logger.LogWarning("Gift not found in CatalogService: {GiftId}", giftId);
            return (false, 0m, 0, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gift from CatalogService: {GiftId}", giftId);
            return (false, 0m, 0, string.Empty);
        }
    }

    public async Task<bool> GiftExistsAndInStockAsync(int giftId, int quantity)
    {
        try
        {
            var (success, _, available, _) = await GetGiftAsync(giftId);
            return success && available >= quantity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking gift inventory: {GiftId}", giftId);
            return false;
        }
    }

    public async Task<bool> ReduceGiftQuantityAsync(int giftId, int quantity)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, $"/catalog/gifts/{giftId}/quantity");
            request.Content = new StringContent($"{{\"quantityChange\": -{quantity}}}",
                System.Text.Encoding.UTF8, "application/json");
            ForwardCorrelationId(request);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Reduced gift quantity: {GiftId} by {Quantity}", giftId, quantity);
                return true;
            }

            _logger.LogWarning("Failed to reduce gift quantity: {GiftId}", giftId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reducing gift quantity: {GiftId}", giftId);
            return false;
        }
    }
}
