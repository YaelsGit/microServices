using MassTransit;
using SharedModels.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Serilog.Context; // 🌟 חובה להוסיף את זה למעלה!

namespace NotificationService.Consumers;

/// <summary>
/// Consumer for OrderCancelled event (COMPENSATION)
/// Published by: OrderService when cancelling an order due to inventory failure
/// Action: Send cancellation email to customer
/// </summary>
public class OrderCancelledConsumer : IConsumer<OrderCancelled>
{
    private readonly ILogger<OrderCancelledConsumer> _logger;

    public OrderCancelledConsumer(ILogger<OrderCancelledConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCancelled> context)
    {
        var message = context.Message;
        
        // 🌟 חילוץ ה-CorrelationId והזרקה שלו ללוגים של הריצה הנוכחית
        var correlationId = context.CorrelationId ?? message.OrderId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogWarning($"[OrderCancelledConsumer] Received OrderCancelled event for Order: {message.OrderId}, User: {message.UserId}, Reason: {message.Reason}");

            try
            {
                // Simulate sending cancellation email
                await SendCancellationEmailAsync(message.OrderId, message.UserId, message.Reason);
                _logger.LogInformation($"[OrderCancelledConsumer] Cancellation email sent for Order: {message.OrderId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[OrderCancelledConsumer] Error sending cancellation email for Order: {message.OrderId}");
                // Don't throw - email failures shouldn't fail the saga
            }
        }
    }

    private async Task SendCancellationEmailAsync(Guid orderId, Guid userId, string reason)
    {
        // In a real implementation, this would integrate with SendGrid, Mailgun, or SMTP
        // For now, just simulate the operation
        await Task.Delay(100);
        _logger.LogWarning($"[EmailSimulation] Cancellation email sent to User: {userId} for Order: {orderId}. Reason: {reason}");
    }
}