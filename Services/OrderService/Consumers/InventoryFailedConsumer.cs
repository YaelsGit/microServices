using MassTransit;
using SharedModels.Events;
using OrderService.Data;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Consumers;

/// <summary>
/// Consumer for InventoryFailed event (COMPENSATION PATH)
/// Published by: InventoryService when stock reservation fails (out of stock)
/// Action: Update order status to "Cancelled" and publish OrderCancelled event
/// </summary>
public class InventoryFailedConsumer : IConsumer<InventoryFailed>
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<InventoryFailedConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public InventoryFailedConsumer(OrderDbContext dbContext, ILogger<InventoryFailedConsumer> logger, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<InventoryFailed> context)
    {
        var message = context.Message;
        _logger.LogError($"[InventoryFailedConsumer] Received InventoryFailed event for Order: {message.OrderId}, Reason: {message.Reason}");

        try
        {
            // Find the order by OrderId
            var guidStr = message.OrderId.ToString("N");
            var intOrderId = (int)Convert.ToInt64(guidStr.Substring(20), 16);
            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == intOrderId);

            if (order == null)
            {
                _logger.LogError($"[InventoryFailedConsumer] Order not found: {message.OrderId}");
                return;
            }

            // Update order status to "Cancelled" (COMPENSATION)
            order.Status = "Cancelled";
            order.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            _logger.LogWarning($"[InventoryFailedConsumer] Order {message.OrderId} status updated to Cancelled due to inventory failure");

            // Publish OrderCancelled event (triggers NotificationService and InventoryService cleanup)
            var cancelledEvent = new OrderCancelled
            {
                OrderId = message.OrderId,
                UserId = order.UserId.ToString() == "" ? Guid.NewGuid() : Guid.Parse(order.UserId.ToString()),
                CancelledAt = DateTime.UtcNow,
                Reason = $"Inventory failed: {message.Reason}"
            };

            await _publishEndpoint.Publish(cancelledEvent);
            _logger.LogInformation($"[InventoryFailedConsumer] OrderCancelled event published for Order: {message.OrderId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[InventoryFailedConsumer] Error processing InventoryFailed event for Order: {message.OrderId}");
            throw;
        }
    }
}
