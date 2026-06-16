using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CatalogService.Data;

public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        
        // Get connection string from environment or use default
        var connectionString = Environment.GetEnvironmentVariable("ASPNETCORE_CONNECTIONSTRING") 
            ?? "Server=localhost\\MSSQLSERVER01;Database=Mechira-sinit-microservices;Integrated Security=true;TrustServerCertificate=true;Encrypt=false;";
        
        optionsBuilder.UseSqlServer(connectionString);
        
        return new CatalogDbContext(optionsBuilder.Options);
    }
}
