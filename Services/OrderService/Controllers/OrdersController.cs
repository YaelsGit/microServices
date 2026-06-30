using Microsoft.AspNetCore.Mvc;
using SharedModels.Dtos;
using OrderService.Interfaces;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrdersService _ordersService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrdersService ordersService, ILogger<OrdersController> logger)
    {
        _ordersService = ordersService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] DtoCreateOrderRequest request)
    {
        _logger.LogInformation("Creating new order for user: {UserId}, gift: {GiftId}", request.UserId, request.GiftId);

        if (request.Quantity <= 0)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Quantity must be greater than 0.",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        var (success, orderId, message) = await _ordersService.CreateOrderAsync(request.UserId, request);

        if (!success)
        {
            _logger.LogWarning("Order creation failed: {Message}", message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        _logger.LogInformation("Order created successfully with ID: {OrderId}", orderId);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<object>
        {
            Success = true,
            Message = message,
            Data = new { OrderId = orderId },
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DtoOrders>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(int id)
    {
        _logger.LogInformation("Fetching order with ID: {OrderId}", id);
        var order = await _ordersService.GetOrderByIdAsync(id);

        if (order == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"Order with ID {id} not found.",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        return Ok(new ApiResponse<DtoOrders>
        {
            Success = true,
            Message = "Order retrieved successfully",
            Data = order,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get all orders for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<List<DtoOrders>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserOrders(int userId)
    {
        _logger.LogInformation("Fetching orders for user: {UserId}", userId);
        var orders = await _ordersService.GetUserOrdersAsync(userId);

        return Ok(new ApiResponse<List<DtoOrders>>
        {
            Success = true,
            Message = "Orders retrieved successfully",
            Data = orders.ToList(),
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Cancel an order
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelOrder(int id)
    {
        _logger.LogInformation("Cancelling order with ID: {OrderId}", id);

        var (success, message) = await _ordersService.CancelOrderAsync(id);

        if (!success)
        {
            _logger.LogWarning("Order cancellation failed: {Message}", message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        _logger.LogInformation("Order cancelled successfully: {OrderId}", id);
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = message,
            Data = null,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get order report (grouped by gift)
    /// </summary>
    [HttpGet("report/by-gift")]
    [ProducesResponseType(typeof(ApiResponse<List<DtoOrderReport>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrderReport()
    {
        _logger.LogInformation("Fetching order report");
        var report = await _ordersService.GetOrdersByGiftAsync();

        return Ok(new ApiResponse<List<DtoOrderReport>>
        {
            Success = true,
            Message = "Order report retrieved successfully",
            Data = report.ToList(),
            Timestamp = DateTime.UtcNow
        });
    }
}
