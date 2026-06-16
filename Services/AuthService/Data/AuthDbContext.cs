using Microsoft.EntityFrameworkCore;
using SharedModels.Models;

namespace AuthService.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map Users to [Auth].[Users] schema
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "Auth");
            
            entity.HasKey(u => u.UserId);
            
            entity.Property(u => u.UserId)
                .ValueGeneratedOnAdd();
            
            entity.Property(u => u.FirstName)
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(u => u.LastName)
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(u => u.Email)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.HasIndex(u => u.Email)
                .IsUnique();
            
            entity.Property(u => u.PasswordHash)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(u => u.PhoneNumber)
                .HasMaxLength(20);
            
            entity.Property(u => u.Role)
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue("user");
            
            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(u => u.UpdatedAt);
        });
    }
}
