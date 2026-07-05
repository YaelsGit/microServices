using Xunit;
using Moq;
using OrderService.Services;
using OrderService.Interfaces;
using OrderService.HttpClients;
using SharedModels.Models;
using SharedModels.Dtos;
using MassTransit;

namespace OrderService.Tests;

public class OrdersServiceTests
{
    private readonly Mock<IOrdersRepository> _repositoryMock;
    private readonly Mock<AuthServiceClient> _authClientMock;
    private readonly Mock<CatalogServiceClient> _catalogClientMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<ILogger<OrdersService>> _loggerMock;
    private readonly OrdersService _service;

    public OrdersServiceTests()
    {
        _repositoryMock = new Mock<IOrdersRepository>();
        _authClientMock = new Mock<AuthServiceClient>();
        _catalogClientMock = new Mock<CatalogServiceClient>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _loggerMock = new Mock<ILogger<OrdersService>>();

        _service = new OrdersService(
            _repositoryMock.Object,
            _authClientMock.Object,
            _catalogClientMock.Object,
            _publishEndpointMock.Object,
            _loggerMock.Object);
    }

    // ── CreateOrderAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateOrderAsync_ValidRequest_ReturnsSuccess()
    {
        _authClientMock.Setup(c => c.UserExistsAsync(1)).ReturnsAsync(true);
        _catalogClientMock.Setup(c => c.GetGiftAsync(1))
            .ReturnsAsync((true, 50m, 10, "Gift A"));
        _repositoryMock.Setup(r => r.CreateOrderAsync(It.IsAny<Order>())).ReturnsAsync(42);
        _publishEndpointMock.Setup(p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new DtoCreateOrderRequest { GiftId = 1, Quantity = 2 };
        var (success, orderId, _) = await _service.CreateOrderAsync(1, request);

        Assert.True(success);
        Assert.Equal(42, orderId);
    }

    [Fact]
    public async Task CreateOrderAsync_ZeroQuantity_ReturnsFail()
    {
        var request = new DtoCreateOrderRequest { GiftId = 1, Quantity = 0 };
        var (success, _, message) = await _service.CreateOrderAsync(1, request);

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    [Fact]
    public async Task CreateOrderAsync_UserNotFound_ReturnsFail()
    {
        _authClientMock.Setup(c => c.UserExistsAsync(99)).ReturnsAsync(false);

        var request = new DtoCreateOrderRequest { GiftId = 1, Quantity = 1 };
        var (success, _, _) = await _service.CreateOrderAsync(99, request);

        Assert.False(success);
    }

    [Fact]
    public async Task CreateOrderAsync_GiftNotFound_ReturnsFail()
    {
        _authClientMock.Setup(c => c.UserExistsAsync(1)).ReturnsAsync(true);
        _catalogClientMock.Setup(c => c.GetGiftAsync(999))
            .ReturnsAsync((false, 0m, 0, string.Empty));

        var request = new DtoCreateOrderRequest { GiftId = 999, Quantity = 1 };
        var (success, _, _) = await _service.CreateOrderAsync(1, request);

        Assert.False(success);
    }

    // ── GetOrderByIdAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderByIdAsync_ExistingOrder_ReturnsDto()
    {
        var order = new Order { OrderId = 5, UserId = 1, GiftId = 1, Quantity = 1, TotalPrice = 50m, Status = "Pending" };
        _repositoryMock.Setup(r => r.GetOrderByIdAsync(5)).ReturnsAsync(order);

        var result = await _service.GetOrderByIdAsync(5);

        Assert.NotNull(result);
        Assert.Equal(5, result!.OrderId);
    }

    [Fact]
    public async Task GetOrderByIdAsync_NonExistent_ReturnsNull()
    {
        _repositoryMock.Setup(r => r.GetOrderByIdAsync(999)).ReturnsAsync((Order?)null);

        var result = await _service.GetOrderByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrderByIdAsync_InvalidId_ReturnsNull()
    {
        var result = await _service.GetOrderByIdAsync(0);
        Assert.Null(result);
    }

    // ── GetUserOrdersAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetUserOrdersAsync_UserExists_ReturnsOrders()
    {
        _authClientMock.Setup(c => c.UserExistsAsync(1)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetOrdersByUserIdAsync(1))
            .ReturnsAsync(new List<Order>
            {
                new Order { OrderId = 1, UserId = 1, GiftId = 1, Quantity = 1, TotalPrice = 50m, Status = "Confirmed" },
                new Order { OrderId = 2, UserId = 1, GiftId = 2, Quantity = 2, TotalPrice = 100m, Status = "Pending" }
            });

        var result = (await _service.GetUserOrdersAsync(1)).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetUserOrdersAsync_UserNotFound_ReturnsEmpty()
    {
        _authClientMock.Setup(c => c.UserExistsAsync(99)).ReturnsAsync(false);

        var result = await _service.GetUserOrdersAsync(99);

        Assert.Empty(result);
    }

    // ── CancelOrderAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CancelOrderAsync_PendingOrder_ReturnsSuccess()
    {
        var order = new Order { OrderId = 1, Status = "Pending" };
        _repositoryMock.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
        _repositoryMock.Setup(r => r.UpdateOrderAsync(It.IsAny<Order>())).ReturnsAsync(true);

        var (success, _) = await _service.CancelOrderAsync(1);

        Assert.True(success);
    }

    [Fact]
    public async Task CancelOrderAsync_ConfirmedOrder_ReturnsFail()
    {
        var order = new Order { OrderId = 1, Status = "Confirmed" };
        _repositoryMock.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

        var (success, message) = await _service.CancelOrderAsync(1);

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    [Fact]
    public async Task CancelOrderAsync_OrderNotFound_ReturnsFail()
    {
        _repositoryMock.Setup(r => r.GetOrderByIdAsync(999)).ReturnsAsync((Order?)null);

        var (success, _) = await _service.CancelOrderAsync(999);

        Assert.False(success);
    }

    // ── GetOrdersByGiftAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetOrdersByGiftAsync_ReturnsGroupedReport()
    {
        _repositoryMock.Setup(r => r.GetAllOrdersAsync())
            .ReturnsAsync(new List<Order>
            {
                new Order { GiftId = 1, Quantity = 2, TotalPrice = 100m, Status = "Confirmed" },
                new Order { GiftId = 1, Quantity = 1, TotalPrice = 50m,  Status = "Confirmed" },
                new Order { GiftId = 2, Quantity = 3, TotalPrice = 150m, Status = "Confirmed" }
            });

        var result = (await _service.GetOrdersByGiftAsync()).ToList();

        Assert.Equal(2, result.Count);
        var gift1 = result.First(r => r.GiftId == 1);
        Assert.Equal(3, gift1.TotalOrdered);
        Assert.Equal(150m, gift1.TotalRevenue);
    }
}
