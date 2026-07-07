using MassTransit;
using SharedModels.Events;
using OrderService.Interfaces;
using OrderService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace OrderService.Consumers;

/// <summary>
/// Consumer for InventoryReserved event (HAPPY PATH)
/// Published by: InventoryService when stock is successfully reserved
/// Action: Update order status to "Confirmed" and publish OrderConfirmed event
/// </summary>
public class InventoryReservedConsumer : IConsumer<InventoryReserved>
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<InventoryReservedConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public InventoryReservedConsumer(OrderDbContext dbContext, ILogger<InventoryReservedConsumer> logger, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<InventoryReserved> context)
    {
        var message = context.Message;

        // 🌟 חילוץ ה-CorrelationId מתוך הקונטקסט של MassTransit (או שימוש ב-OrderId כגיבוי)
        var correlationId = context.CorrelationId ?? message.OrderId;

        // 🌟 עטיפת הלוגיקה באמצעות ה-LogContext המפורש של Serilog
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogInformation($"[InventoryReservedConsumer] Received InventoryReserved event for Order: {message.OrderId}");

            try
            {
                // Extract int OrderId from Guid (last 12 hex chars)
                var guidStr = message.OrderId.ToString("N");
                var intOrderId = (int)Convert.ToInt64(guidStr.Substring(20), 16);
                var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == intOrderId);

                if (order == null)
                {
                    _logger.LogError($"[InventoryReservedConsumer] Order not found: {message.OrderId}");
                    return;
                }

                // Update order status to "Confirmed"
                order.Status = "Confirmed";
                order.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"[InventoryReservedConsumer] Order {message.OrderId} status updated to Confirmed");

                // Publish OrderConfirmed event
                var confirmedEvent = new OrderConfirmed
                {
                    OrderId = message.OrderId,
                    UserId = order.UserId.ToString() == "" ? Guid.NewGuid() : Guid.Parse(order.UserId.ToString()),
                    ConfirmedAt = DateTime.UtcNow
                };

                await _publishEndpoint.Publish(confirmedEvent);
                _logger.LogInformation($"[InventoryReservedConsumer] OrderConfirmed event published for Order: {message.OrderId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[InventoryReservedConsumer] Error processing InventoryReserved event for Order: {message.OrderId}");
                throw;
            }
        }
    }
}