using MassTransit;
using SharedModels.Events;
using CatalogService.Interfaces;

namespace CatalogService.Consumers;

/// <summary>
/// Consumes OrderPlaced events from RabbitMQ.
/// Uses IGiftsRepository (MongoDB) — not SQL — for inventory checks.
/// Publishes InventoryReserved (happy path) or InventoryFailed (compensation).
/// </summary>
public class OrderPlacedConsumer : IConsumer<OrderPlaced>
{
    private readonly IGiftsRepository _giftsRepository;
    private readonly ILogger<OrderPlacedConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderPlacedConsumer(IGiftsRepository giftsRepository, ILogger<OrderPlacedConsumer> logger, IPublishEndpoint publishEndpoint)
    {
        _giftsRepository = giftsRepository;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var message = context.Message;
        var correlationId = context.CorrelationId ?? message.OrderId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogInformation("[OrderPlacedConsumer] Received OrderPlaced for Order: {OrderId}", message.OrderId);

            try
            {
                var reservedItems = new List<ReservedItemDto>();

                foreach (var item in message.Items)
                {
                    // Convert ProductId (Guid) to GiftId (int)
                    var giftIdString = item.ProductId.ToString("N");
                    if (!int.TryParse(giftIdString[..8], System.Globalization.NumberStyles.HexNumber, null, out var giftId))
                        giftId = Math.Abs(item.ProductId.GetHashCode() % 10000);

                    // Read from MongoDB via repository
                    var gift = await _giftsRepository.GetGiftByIdAsync(giftId);

                    if (gift == null)
                    {
                        _logger.LogError("[OrderPlacedConsumer] Gift not found: {GiftId}", giftId);
                        await PublishFailed(message.OrderId, $"Gift not found: {giftId}");
                        return;
                    }

                    if (gift.Quantity < item.Quantity)
                    {
                        _logger.LogWarning("[OrderPlacedConsumer] Insufficient stock for GiftId: {GiftId}. Available: {Available}, Requested: {Requested}",
                            giftId, gift.Quantity, item.Quantity);
                        await PublishFailed(message.OrderId, $"Insufficient stock. Available: {gift.Quantity}, Requested: {item.Quantity}");
                        return;
                    }

                    // Reserve stock in MongoDB
                    await _giftsRepository.UpdateGiftQuantityAsync(giftId, -item.Quantity);
                    _logger.LogInformation("[OrderPlacedConsumer] Stock reserved in MongoDB for GiftId: {GiftId}. Remaining: {Remaining}",
                        giftId, gift.Quantity - item.Quantity);

                    reservedItems.Add(new ReservedItemDto { ProductId = item.ProductId, Quantity = item.Quantity });
                }

                await _publishEndpoint.Publish(new InventoryReserved
                {
                    OrderId = message.OrderId,
                    ReservedAt = DateTime.UtcNow,
                    ReservedItems = reservedItems
                });
                _logger.LogInformation("[OrderPlacedConsumer] InventoryReserved published for Order: {OrderId}", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OrderPlacedConsumer] Unexpected error for Order: {OrderId}", message.OrderId);
                await PublishFailed(message.OrderId, $"Unexpected error: {ex.Message}");
            }
        }
    }

    private async Task PublishFailed(Guid orderId, string reason)
    {
        try
        {
            await _publishEndpoint.Publish(new InventoryFailed
            {
                OrderId = orderId,
                FailedAt = DateTime.UtcNow,
                Reason = reason
            });
            _logger.LogWarning("[OrderPlacedConsumer] InventoryFailed published for Order: {OrderId} — {Reason}", orderId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OrderPlacedConsumer] Failed to publish InventoryFailed for Order: {OrderId}", orderId);
        }
    }
}