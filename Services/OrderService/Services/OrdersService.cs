using SharedModels.Models;
using SharedModels.Dtos;
using SharedModels.Constants;
using OrderService.Interfaces;
using OrderService.HttpClients;

namespace OrderService.Services;

/// <summary>
/// Service implementation for Order business logic
/// Handles CRUD operations and inter-service communication for order processing
/// Depends on: OrdersRepository, AuthServiceClient, CatalogServiceClient
/// </summary>
public class OrdersService : IOrdersService
{
    private readonly IOrdersRepository _repository;
    private readonly AuthServiceClient _authServiceClient;
    private readonly CatalogServiceClient _catalogServiceClient;
    private readonly ILogger<OrdersService> _logger;

    public OrdersService(
        IOrdersRepository repository,
        AuthServiceClient authServiceClient,
        CatalogServiceClient catalogServiceClient,
        ILogger<OrdersService> logger)
    {
        _repository = repository;
        _authServiceClient = authServiceClient;
        _catalogServiceClient = catalogServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Create new order with validation
    /// Business logic: 
    /// 1. Validate user exists (via AuthService)
    /// 2. Validate gift exists and has inventory (via CatalogService)
    /// 3. Create order in repository
    /// 4. Reduce gift quantity in CatalogService
    /// </summary>
    public async Task<(bool Success, int OrderId, string Message)> CreateOrderAsync(int userId, DtoCreateOrderRequest request)
    {
        try
        {
            _logger.LogInformation($"Creating order for user: {userId}");

            // Validate input
            if (request.Quantity <= 0)
            {
                _logger.LogWarning("Create order attempt with invalid quantity");
                return (false, 0, "Order quantity must be greater than 0");
            }

            // Step 1: Validate user exists via AuthService
            var userExists = await _authServiceClient.UserExistsAsync(userId);
            if (!userExists)
            {
                _logger.LogWarning($"Order creation for non-existent user: {userId}");
                return (false, 0, ErrorCodes.GetErrorMessage(ErrorCodes.USER_NOT_FOUND));
            }

            // Step 2: Validate gift exists and has inventory via CatalogService
            var giftInStock = await _catalogServiceClient.GiftExistsAndInStockAsync(request.GiftId, request.Quantity);
            if (!giftInStock)
            {
                _logger.LogWarning($"Gift not available or out of stock: {request.GiftId}");
                return (false, 0, ErrorCodes.GetErrorMessage(ErrorCodes.INSUFFICIENT_STOCK));
            }

            // Get gift details for price calculation
            var (giftFound, price, _, giftName) = await _catalogServiceClient.GetGiftAsync(request.GiftId);
            if (!giftFound)
            {
                return (false, 0, ErrorCodes.GetErrorMessage(ErrorCodes.GIFT_NOT_FOUND));
            }

            // Step 3: Create order in database
            var order = new Order
            {
                UserId = userId,
                GiftId = request.GiftId,
                Quantity = request.Quantity,
                TotalPrice = price * request.Quantity,
                Status = "pending"
            };

            var orderId = await _repository.CreateOrderAsync(order);

            // Step 4: Reduce gift quantity in CatalogService
            var quantityReduced = await _catalogServiceClient.ReduceGiftQuantityAsync(request.GiftId, request.Quantity);
            if (!quantityReduced)
            {
                _logger.LogWarning($"Failed to reduce gift quantity, but order was created: {orderId}");
                // Note: In production, consider compensating transaction or manual intervention
            }

            _logger.LogInformation($"Order created successfully: {orderId}");
            return (true, orderId, "Order created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating order for user: {userId}");
            return (false, 0, "An error occurred while creating the order");
        }
    }

    /// <summary>
    /// Get all orders for a user
    /// </summary>
    public async Task<IEnumerable<DtoOrders>> GetUserOrdersAsync(int userId)
    {
        try
        {
            // Validate user exists
            var userExists = await _authServiceClient.UserExistsAsync(userId);
            if (!userExists)
            {
                _logger.LogWarning($"Get orders for non-existent user: {userId}");
                return Enumerable.Empty<DtoOrders>();
            }

            var orders = await _repository.GetOrdersByUserIdAsync(userId);
            return orders.Select(o => MapToDto(o));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting orders for user: {userId}");
            throw;
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    public async Task<DtoOrders?> GetOrderByIdAsync(int orderId)
    {
        try
        {
            if (orderId <= 0)
                return null;

            var order = await _repository.GetOrderByIdAsync(orderId);
            return order == null ? null : MapToDto(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting order: {orderId}");
            throw;
        }
    }

    /// <summary>
    /// Cancel order
    /// Business logic: Only allow cancellation of pending orders
    /// </summary>
    public async Task<(bool Success, string Message)> CancelOrderAsync(int orderId)
    {
        try
        {
            var order = await _repository.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning($"Cancel order attempt for non-existent order: {orderId}");
                return (false, "Order not found");
            }

            // Business logic: Only cancel if pending
            if (order.Status != "pending")
            {
                _logger.LogWarning($"Cannot cancel order with status '{order.Status}': {orderId}");
                return (false, $"Cannot cancel order with status '{order.Status}'");
            }

            // Update order status
            order.Status = "cancelled";
            var success = await _repository.UpdateOrderAsync(order);

            if (success)
            {
                _logger.LogInformation($"Order cancelled: {orderId}");
                // TODO: Restore gift quantity in CatalogService
                return (true, "Order cancelled successfully");
            }

            return (false, "Failed to cancel order");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error cancelling order: {orderId}");
            return (false, "An error occurred while cancelling the order");
        }
    }

    /// <summary>
    /// Get order report by gift
    /// </summary>
    public async Task<IEnumerable<DtoOrderReport>> GetOrdersByGiftAsync()
    {
        try
        {
            var orders = await _repository.GetAllOrdersAsync();
            
            // Group by gift and calculate totals
            var reports = orders
                .Where(o => o.Status == "completed")
                .GroupBy(o => o.GiftId)
                .Select(g => new DtoOrderReport
                {
                    GiftId = g.Key,
                    GiftName = $"Gift {g.Key}", // Would be populated from CatalogService
                    TotalOrdered = g.Sum(o => o.Quantity),
                    TotalRevenue = g.Sum(o => o.TotalPrice)
                })
                .ToList();

            return reports;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order report by gift");
            throw;
        }
    }

    /// <summary>
    /// Helper method to map Order to DtoOrders
    /// </summary>
    private DtoOrders MapToDto(Order order)
    {
        return new DtoOrders
        {
            OrderId = order.OrderId,
            UserId = order.UserId,
            GiftId = order.GiftId,
            Quantity = order.Quantity,
            TotalPrice = order.TotalPrice,
            Status = order.Status,
            UserEmail = string.Empty, // Would be populated from AuthService
            GiftName = string.Empty, // Would be populated from CatalogService
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }
}
