using SharedModels.Dtos;

namespace OrderService.Interfaces;

/// <summary>
/// Service interface for Order business logic
/// Defines contracts for order operations and inter-service communication
/// </summary>
public interface IOrdersService
{
    Task<(bool Success, int OrderId, string Message)> CreateOrderAsync(int userId, DtoCreateOrderRequest request);
    Task<IEnumerable<DtoOrders>> GetUserOrdersAsync(int userId);
    Task<DtoOrders?> GetOrderByIdAsync(int orderId);
    Task<bool> CancelOrderAsync(int orderId);
    Task<IEnumerable<DtoOrderReport>> GetOrdersByGiftAsync();
}
