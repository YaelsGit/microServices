using Microsoft.EntityFrameworkCore;
using SharedModels.Models;

namespace LotteryService.Data;

public class LotteryDbContext : DbContext
{
    public LotteryDbContext(DbContextOptions<LotteryDbContext> options) : base(options)
    {
    }

    public DbSet<Lottery> Lotteries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map Lotteries to [Lottery].[Lottery] schema
        modelBuilder.Entity<Lottery>(entity =>
        {
            entity.ToTable("Lottery", "Lottery");
            
            entity.HasKey(l => l.LotteryId);
            
            entity.Property(l => l.LotteryId)
                .ValueGeneratedOnAdd();
            
            // Shadow ForeignKeys - UserId and GiftId are properties but NOT navigation properties
            entity.Property(l => l.UserId)
                .IsRequired();
            
            entity.Property(l => l.GiftId)
                .IsRequired();
            
            entity.Property(l => l.Status)
                .HasMaxLength(50)
                .IsRequired()
                .HasDefaultValue("active");
            
            entity.Property(l => l.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(l => l.WonAt);
            
            // Create indexes for common queries
            entity.HasIndex(l => l.UserId);
            entity.HasIndex(l => l.GiftId);
            entity.HasIndex(l => l.Status);
            entity.HasIndex(l => l.CreatedAt);
        });
    }
}
