using Microsoft.EntityFrameworkCore;
using SharedModels.Models;

namespace CatalogService.Data;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Donor> Donors { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Gift> Gifts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map Donors to [Catalog].[Donors] schema
        modelBuilder.Entity<Donor>(entity =>
        {
            entity.ToTable("Donors", "Catalog");
            
            entity.HasKey(d => d.DonorId);
            
            entity.Property(d => d.DonorId)
                .ValueGeneratedOnAdd();
            
            entity.Property(d => d.Name)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(d => d.Email)
                .HasMaxLength(255);
            
            entity.Property(d => d.PhoneNumber)
                .HasMaxLength(20);
            
            entity.Property(d => d.Address)
                .HasMaxLength(500);
            
            entity.Property(d => d.City)
                .HasMaxLength(100);
            
            entity.Property(d => d.Country)
                .HasMaxLength(100);
            
            entity.Property(d => d.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(d => d.UpdatedAt);
        });

        // Map Categories to [Catalog].[Categories] schema
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories", "Catalog");
            
            entity.HasKey(c => c.CategoryId);
            
            entity.Property(c => c.CategoryId)
                .ValueGeneratedOnAdd();
            
            entity.Property(c => c.Name)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(c => c.Description)
                .HasMaxLength(1000);
            
            entity.Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(c => c.UpdatedAt);
        });

        // Map Gifts to [Catalog].[Gifts] schema
        modelBuilder.Entity<Gift>(entity =>
        {
            entity.ToTable("Gifts", "Catalog");
            
            entity.HasKey(g => g.GiftId);
            
            entity.Property(g => g.GiftId)
                .ValueGeneratedOnAdd();
            
            entity.Property(g => g.Name)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(g => g.Description)
                .HasMaxLength(1000);
            
            entity.Property(g => g.Price)
                .HasPrecision(18, 2)
                .IsRequired();
            
            entity.Property(g => g.Quantity)
                .IsRequired()
                .HasDefaultValue(0);
            
            entity.Property(g => g.DonorId)
                .IsRequired();
            
            entity.Property(g => g.CategoryId)
                .IsRequired();
            
            // Shadow ForeignKeys - no navigation properties
            entity.Property<int>("DonorId");
            entity.Property<int>("CategoryId");
            
            entity.Property(g => g.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(g => g.UpdatedAt);
            
            // Create indexes for common queries
            entity.HasIndex(g => g.DonorId);
            entity.HasIndex(g => g.CategoryId);
            entity.HasIndex(g => g.Price);
        });
    }
}
