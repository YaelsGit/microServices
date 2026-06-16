using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LotteryService.Data;

public class LotteryDbContextFactory : IDesignTimeDbContextFactory<LotteryDbContext>
{
    public LotteryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LotteryDbContext>();
        
        // Get connection string from environment or use default
        var connectionString = Environment.GetEnvironmentVariable("ASPNETCORE_CONNECTIONSTRING") 
            ?? "Server=localhost\\MSSQLSERVER01;Database=Mechira-sinit-microservices;Integrated Security=true;TrustServerCertificate=true;Encrypt=false;";
        
        optionsBuilder.UseSqlServer(connectionString);
        
        return new LotteryDbContext(optionsBuilder.Options);
    }
}
