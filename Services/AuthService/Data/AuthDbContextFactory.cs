using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthService.Data;

public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        
        // Get connection string from environment or use default
        var connectionString = Environment.GetEnvironmentVariable("ASPNETCORE_CONNECTIONSTRING") 
            ?? "Server=localhost\\MSSQLSERVER01;Database=Mechira-sinit-microservices;Integrated Security=true;TrustServerCertificate=true;Encrypt=false;";
        
        optionsBuilder.UseSqlServer(connectionString);
        
        return new AuthDbContext(optionsBuilder.Options);
    }
}
