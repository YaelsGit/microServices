namespace SharedModels.Interfaces;

public interface IOrderService
{
    Task<int> CreateOrderAsync(int userId, SharedModels.Dtos.DtoCreateOrderRequest request);
    Task<IEnumerable<SharedModels.Dtos.DtoOrders>> GetUserOrdersAsync(int userId);
    Task<SharedModels.Dtos.DtoOrders?> GetOrderByIdAsync(int orderId);
    Task<bool> CancelOrderAsync(int orderId);
    Task<IEnumerable<SharedModels.Dtos.DtoOrderReport>> GetOrdersByGiftAsync();
    Task<IEnumerable<SharedModels.Dtos.DtoOrderByPriceRange>> GetOrdersByPriceRangeAsync();
}
