using MassTransit;
using SharedModels.Models;
using SharedModels.Dtos;
using SharedModels.Constants;
using SharedModels.Events;
using OrderService.Interfaces;
using OrderService.HttpClients;

namespace OrderService.Services;

/// <summary>
/// Order business logic.
/// CreateOrderAsync follows the Saga pattern:
///   1. Validate user + gift via HTTP (synchronous pre-checks)
///   2. Persist order with status "Pending"
///   3. Publish OrderPlaced → CatalogService reserves inventory async
///   4. InventoryReservedConsumer / InventoryFailedConsumer update status
/// </summary>
public class OrdersService : IOrdersService
{
    private readonly IOrdersRepository _repository;
    private readonly AuthServiceClient _authServiceClient;
    private readonly CatalogServiceClient _catalogServiceClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrdersService> _logger;

    public OrdersService(
        IOrdersRepository repository,
        AuthServiceClient authServiceClient,
        CatalogServiceClient catalogServiceClient,
        IPublishEndpoint publishEndpoint,
        ILogger<OrdersService> logger)
    {
        _repository = repository;
        _authServiceClient = authServiceClient;
        _catalogServiceClient = catalogServiceClient;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Creates an order and kicks off the inventory-reservation saga.
    /// Returns immediately after publishing OrderPlaced — status stays "Pending"
    /// until CatalogService responds via RabbitMQ.
    /// </summary>
    public async Task<(bool Success, int OrderId, string Message)> CreateOrderAsync(int userId, DtoCreateOrderRequest request)
    {
        try
        {
            if (request.Quantity <= 0)
                return (false, 0, "Order quantity must be greater than 0");

            // Step 1: Validate user exists
            var userExists = await _authServiceClient.UserExistsAsync(userId);
            if (!userExists)
            {
                _logger.LogWarning("Order creation for non-existent user: {UserId}", userId);
                return (false, 0, ErrorCodes.GetErrorMessage(ErrorCodes.USER_NOT_FOUND));
            }

            // Step 2: Get gift price (lightweight check — inventory reservation happens async via saga)
            var (giftFound, price, _, giftName) = await _catalogServiceClient.GetGiftAsync(request.GiftId);
            if (!giftFound)
            {
                _logger.LogWarning("Gift not found: {GiftId}", request.GiftId);
                return (false, 0, ErrorCodes.GetErrorMessage(ErrorCodes.GIFT_NOT_FOUND));
            }

            // Step 3: Persist order as "Pending" — saga will update status
            var correlationId = Guid.NewGuid();
            var order = new Order
            {
                UserId = userId,
                GiftId = request.GiftId,
                Quantity = request.Quantity,
                TotalPrice = price * request.Quantity,
                Status = "Pending"
            };

            var orderId = await _repository.CreateOrderAsync(order);
            _logger.LogInformation("[SAGA] Order {OrderId} created with status Pending. CorrelationId: {CorrelationId}", orderId, correlationId);

            // Step 4: Publish OrderPlaced — CatalogService will reserve inventory
            await _publishEndpoint.Publish(new OrderPlaced
            {
                OrderId = correlationId,
                UserId = Guid.NewGuid(), // bridge: int userId → Guid for event
                CreatedAt = DateTime.UtcNow,
                TotalPrice = order.TotalPrice,
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = Guid.Parse(request.GiftId.ToString("D32").PadLeft(32, '0').Insert(8,"-").Insert(13,"-").Insert(18,"-").Insert(23,"-")),
                        Quantity = request.Quantity,
                        Price = price
                    }
                }
            }, ctx => ctx.CorrelationId = correlationId);

            _logger.LogInformation("[SAGA] OrderPlaced event published for Order {OrderId}", orderId);
            return (true, orderId, "Order created — awaiting inventory confirmation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for user: {UserId}", userId);
            return (false, 0, "An error occurred while creating the order");
        }
    }

    public async Task<IEnumerable<DtoOrders>> GetUserOrdersAsync(int userId)
    {
        var userExists = await _authServiceClient.UserExistsAsync(userId);
        if (!userExists)
        {
            _logger.LogWarning("Get orders for non-existent user: {UserId}", userId);
            return Enumerable.Empty<DtoOrders>();
        }

        var orders = await _repository.GetOrdersByUserIdAsync(userId);
        return orders.Select(MapToDto);
    }

    public async Task<DtoOrders?> GetOrderByIdAsync(int orderId)
    {
        if (orderId <= 0) return null;
        var order = await _repository.GetOrderByIdAsync(orderId);
        return order == null ? null : MapToDto(order);
    }

    public async Task<(bool Success, string Message)> CancelOrderAsync(int orderId)
    {
        var order = await _repository.GetOrderByIdAsync(orderId);
        if (order == null)
            return (false, "Order not found");

        if (order.Status != "Pending")
            return (false, $"Cannot cancel order with status '{order.Status}'");

        order.Status = "Cancelled";
        var success = await _repository.UpdateOrderAsync(order);
        if (success)
            _logger.LogInformation("Order {OrderId} cancelled by user request", orderId);

        return success ? (true, "Order cancelled successfully") : (false, "Failed to cancel order");
    }

    public async Task<IEnumerable<DtoOrderReport>> GetOrdersByGiftAsync()
    {
        var orders = await _repository.GetAllOrdersAsync();
        return orders
            .Where(o => o.Status == "Confirmed")
            .GroupBy(o => o.GiftId)
            .Select(g => new DtoOrderReport
            {
                GiftId = g.Key,
                GiftName = $"Gift {g.Key}",
                TotalOrdered = g.Sum(o => o.Quantity),
                TotalRevenue = g.Sum(o => o.TotalPrice)
            });
    }

    private static DtoOrders MapToDto(Order order) => new()
    {
        OrderId = order.OrderId,
        UserId = order.UserId,
        GiftId = order.GiftId,
        Quantity = order.Quantity,
        TotalPrice = order.TotalPrice,
        Status = order.Status,
        UserEmail = string.Empty,
        GiftName = string.Empty,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt
    };
}
