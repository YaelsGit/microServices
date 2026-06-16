using Microsoft.EntityFrameworkCore;
using SharedModels.Models;
using OrderService.Data;
using OrderService.Interfaces;

namespace OrderService.Repository;

/// <summary>
/// Repository implementation for Order data access
/// PURE data access layer: handles ONLY database operations
/// Business logic (order validation, status rules, quantity checks) 
/// is implemented in OrdersService - NOT here
/// Note: No direct joins to User/Gift tables - only stores UserId and GiftId
/// </summary>
public class OrdersRepository : IOrdersRepository
{
    private readonly OrderDbContext _context;

    public OrdersRepository(OrderDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        return await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    /// <summary>
    /// Get all orders for a specific user
    /// </summary>
    public async Task<List<Order>> GetOrdersByUserIdAsync(int userId)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get all orders in the system (admin view)
    /// </summary>
    public async Task<List<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Create new order
    /// Returns the generated OrderId
    /// </summary>
    public async Task<int> CreateOrderAsync(Order order)
    {
        order.CreatedAt = DateTime.UtcNow;
        
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        return order.OrderId;
    }

    /// <summary>
    /// Update existing order (status change, etc.)
    /// </summary>
    public async Task<bool> UpdateOrderAsync(Order order)
    {
        order.UpdatedAt = DateTime.UtcNow;
        
        _context.Orders.Update(order);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Cancel an order by changing status to "cancelled"
    /// Note: Business logic validation (e.g., checking if order is already completed)
    /// is handled by the Service layer, not here
    /// </summary>
    public async Task<bool> CancelOrderAsync(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return false;

        order.Status = "cancelled";
        order.UpdatedAt = DateTime.UtcNow;

        _context.Orders.Update(order);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Delete order by ID (soft delete recommended in production)
    /// </summary>
    public async Task<bool> DeleteOrderAsync(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return false;

        _context.Orders.Remove(order);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Get orders by status (pending, completed, cancelled)
    /// </summary>
    public async Task<List<Order>> GetOrdersByStatusAsync(string status)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get orders within a date range
    /// </summary>
    public async Task<List<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Calculate total revenue from all orders
    /// </summary>
    public async Task<decimal> GetTotalRevenueAsync()
    {
        return await _context.Orders
            .Where(o => o.Status == "completed")
            .SumAsync(o => o.TotalPrice);
    }

    /// <summary>
    /// Get total count of all orders
    /// </summary>
    public async Task<int> GetTotalOrdersCountAsync()
    {
        return await _context.Orders.CountAsync();
    }
}
