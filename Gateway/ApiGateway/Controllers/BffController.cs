using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ApiGateway.Controllers;

/// <summary>
/// BFF (Backend for Frontend) Controller.
/// Aggregates data from multiple services into a single response,
/// shaped for the web client — eliminating multiple round-trips.
/// </summary>
[ApiController]
[Route("bff")]
public class BffController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BffController> _logger;

    public BffController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<BffController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// GET /bff/order-details/{orderId}
    /// Aggregates: OrderService (order data) + CatalogService (gift data)
    /// Returns a single enriched response for the order details page.
    /// </summary>
    [HttpGet("order-details/{orderId:int}")]
    public async Task<IActionResult> GetOrderDetails(int orderId)
    {
        _logger.LogInformation("[BFF] GetOrderDetails called for OrderId: {OrderId}", orderId);

        var orderServiceUrl = _configuration["ServiceUrls:OrderService"] ?? "http://order-service:5003";
        var catalogServiceUrl = _configuration["ServiceUrls:CatalogService"] ?? "http://catalog-service:5002";

        // Forward the Authorization header to downstream services
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();

        var client = _httpClientFactory.CreateClient();
        if (!string.IsNullOrEmpty(authHeader))
            client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authHeader);

        // Fetch order and gift data in parallel
        var orderTask = client.GetAsync($"{orderServiceUrl}/api/orders/{orderId}");
        
        // We need the giftId from the order first — fetch order, then gift
        // For parallel efficiency: fetch order first, then gift concurrently with any other data
        HttpResponseMessage orderResponse;
        try
        {
            orderResponse = await orderTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BFF] Failed to reach OrderService for OrderId: {OrderId}", orderId);
            return StatusCode(503, new { error = "OrderService unavailable" });
        }

        if (!orderResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("[BFF] OrderService returned {StatusCode} for OrderId: {OrderId}", orderResponse.StatusCode, orderId);
            return StatusCode((int)orderResponse.StatusCode, new { error = "Order not found" });
        }

        var orderJson = await orderResponse.Content.ReadAsStringAsync();
        using var orderDoc = JsonDocument.Parse(orderJson);
        var orderData = orderDoc.RootElement.GetProperty("data");
        var giftId = orderData.GetProperty("giftId").GetInt32();

        // Now fetch gift data from CatalogService
        HttpResponseMessage giftResponse;
        try
        {
            giftResponse = await client.GetAsync($"{catalogServiceUrl}/api/gifts/{giftId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BFF] Failed to reach CatalogService for GiftId: {GiftId}", giftId);
            // Return partial data — order exists, gift data unavailable
            return Ok(new
            {
                orderId = orderData.GetProperty("orderId").GetInt32(),
                userId = orderData.GetProperty("userId").GetInt32(),
                giftId,
                quantity = orderData.GetProperty("quantity").GetInt32(),
                totalPrice = orderData.GetProperty("totalPrice").GetDecimal(),
                status = orderData.GetProperty("status").GetString(),
                createdAt = orderData.GetProperty("createdAt").GetDateTime(),
                gift = (object?)null,
                warning = "Gift details temporarily unavailable"
            });
        }

        object? giftDetails = null;
        if (giftResponse.IsSuccessStatusCode)
        {
            var giftJson = await giftResponse.Content.ReadAsStringAsync();
            using var giftDoc = JsonDocument.Parse(giftJson);
            var giftData = giftDoc.RootElement.GetProperty("data");
            giftDetails = new
            {
                giftId = giftData.GetProperty("giftId").GetInt32(),
                name = giftData.GetProperty("name").GetString(),
                description = giftData.GetProperty("description").GetString(),
                price = giftData.GetProperty("price").GetDecimal(),
                quantity = giftData.GetProperty("quantity").GetInt32()
            };
        }

        _logger.LogInformation("[BFF] Successfully aggregated order + gift data for OrderId: {OrderId}", orderId);

        return Ok(new
        {
            orderId = orderData.GetProperty("orderId").GetInt32(),
            userId = orderData.GetProperty("userId").GetInt32(),
            quantity = orderData.GetProperty("quantity").GetInt32(),
            totalPrice = orderData.GetProperty("totalPrice").GetDecimal(),
            status = orderData.GetProperty("status").GetString(),
            createdAt = orderData.GetProperty("createdAt").GetDateTime(),
            gift = giftDetails
        });
    }

    /// <summary>
    /// GET /bff/user-dashboard/{userId}
    /// Aggregates: all orders for a user + gift details for each order.
    /// Returns enriched order list for the user dashboard page.
    /// </summary>
    [HttpGet("user-dashboard/{userId:int}")]
    public async Task<IActionResult> GetUserDashboard(int userId)
    {
        _logger.LogInformation("[BFF] GetUserDashboard called for UserId: {UserId}", userId);

        var orderServiceUrl = _configuration["ServiceUrls:OrderService"] ?? "http://order-service:5003";
        var catalogServiceUrl = _configuration["ServiceUrls:CatalogService"] ?? "http://catalog-service:5002";

        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        var client = _httpClientFactory.CreateClient();
        if (!string.IsNullOrEmpty(authHeader))
            client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authHeader);

        // Fetch all orders for user
        HttpResponseMessage ordersResponse;
        try
        {
            ordersResponse = await client.GetAsync($"{orderServiceUrl}/api/orders/user/{userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BFF] Failed to reach OrderService for UserId: {UserId}", userId);
            return StatusCode(503, new { error = "OrderService unavailable" });
        }

        if (!ordersResponse.IsSuccessStatusCode)
            return StatusCode((int)ordersResponse.StatusCode, new { error = "Could not fetch orders" });

        var ordersJson = await ordersResponse.Content.ReadAsStringAsync();
        using var ordersDoc = JsonDocument.Parse(ordersJson);
        var ordersArray = ordersDoc.RootElement.GetProperty("data");

        // Collect unique giftIds and fetch them in parallel
        var giftIds = ordersArray.EnumerateArray()
            .Select(o => o.GetProperty("giftId").GetInt32())
            .Distinct()
            .ToList();

        var giftTasks = giftIds.Select(gid =>
            client.GetAsync($"{catalogServiceUrl}/api/gifts/{gid}")
                  .ContinueWith(t => (gid, response: t.Result)));

        var giftResults = await Task.WhenAll(giftTasks);

        var giftMap = new Dictionary<int, object?>();
        foreach (var (gid, response) in giftResults)
        {
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var d = doc.RootElement.GetProperty("data");
                giftMap[gid] = new
                {
                    giftId = d.GetProperty("giftId").GetInt32(),
                    name = d.GetProperty("name").GetString(),
                    price = d.GetProperty("price").GetDecimal()
                };
            }
        }

        var enrichedOrders = ordersArray.EnumerateArray().Select(o =>
        {
            var gid = o.GetProperty("giftId").GetInt32();
            return new
            {
                orderId = o.GetProperty("orderId").GetInt32(),
                giftId = gid,
                quantity = o.GetProperty("quantity").GetInt32(),
                totalPrice = o.GetProperty("totalPrice").GetDecimal(),
                status = o.GetProperty("status").GetString(),
                createdAt = o.GetProperty("createdAt").GetDateTime(),
                gift = giftMap.GetValueOrDefault(gid)
            };
        }).ToList();

        _logger.LogInformation("[BFF] Returning {Count} enriched orders for UserId: {UserId}", enrichedOrders.Count, userId);

        return Ok(new { userId, orders = enrichedOrders });
    }
}
