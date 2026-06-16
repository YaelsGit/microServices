using SharedModels.Models;

namespace OrderService.Interfaces;

/// <summary>
/// Repository interface for Order data access in OrderService
/// </summary>
public interface IOrdersRepository
{
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task<List<Order>> GetOrdersByUserIdAsync(int userId);
    Task<List<Order>> GetAllOrdersAsync();
    Task<int> CreateOrderAsync(Order order);
    Task<bool> UpdateOrderAsync(Order order);
    Task<bool> CancelOrderAsync(int orderId);
    Task<bool> DeleteOrderAsync(int orderId);
    
    // Reporting methods
    Task<List<Order>> GetOrdersByStatusAsync(string status);
    Task<List<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<decimal> GetTotalRevenueAsync();
    Task<int> GetTotalOrdersCountAsync();
}
