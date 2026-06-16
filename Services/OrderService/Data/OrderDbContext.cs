using Microsoft.EntityFrameworkCore;
using SharedModels.Models;

namespace OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map Orders to [Orders].[Orders] schema
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders", "Orders");
            
            entity.HasKey(o => o.OrderId);
            
            entity.Property(o => o.OrderId)
                .ValueGeneratedOnAdd();
            
            // Shadow ForeignKeys - UserId and GiftId are properties but NOT navigation properties
            // This ensures we cannot accidentally join across services
            entity.Property(o => o.UserId)
                .IsRequired();
            
            entity.Property(o => o.GiftId)
                .IsRequired();
            
            entity.Property(o => o.Quantity)
                .IsRequired()
                .HasDefaultValue(1);
            
            entity.Property(o => o.TotalPrice)
                .HasPrecision(18, 2)
                .IsRequired();
            
            entity.Property(o => o.Status)
                .HasMaxLength(50)
                .IsRequired()
                .HasDefaultValue("pending");
            
            entity.Property(o => o.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(o => o.UpdatedAt);
            
            // Create indexes for common queries
            entity.HasIndex(o => o.UserId);
            entity.HasIndex(o => o.GiftId);
            entity.HasIndex(o => o.Status);
            entity.HasIndex(o => o.CreatedAt);
        });
    }
}
