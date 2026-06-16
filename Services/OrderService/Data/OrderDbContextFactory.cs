using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderService.Data;

public class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrderDbContext>();
        
        // Get connection string from environment or use default
        var connectionString = Environment.GetEnvironmentVariable("ASPNETCORE_CONNECTIONSTRING") 
            ?? "Server=localhost\\MSSQLSERVER01;Database=Mechira-sinit-microservices;Integrated Security=true;TrustServerCertificate=true;Encrypt=false;";
        
        optionsBuilder.UseSqlServer(connectionString);
        
        return new OrderDbContext(optionsBuilder.Options);
    }
}
