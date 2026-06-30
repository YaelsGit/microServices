# Phase 8: Verification Checklist & Quick Reference

## ✅ Verification Checklist

### Program.cs Configuration

#### AuthService (Port 5001)
- ✅ Listens on http://localhost:5001
- ✅ AuthDbContext registered with DbContextOptions
- ✅ UsersRepository registered as scoped
- ✅ UsersService registered as scoped
- ✅ JwtTokenService registered as singleton
- ✅ JWT Bearer authentication configured
- ✅ CORS policy "AllowAll" configured
- ✅ Serilog logging with file rolling
- ✅ CorrelationIdMiddleware added
- ✅ GlobalExceptionMiddleware added
- ✅ Swagger UI configured
- ✅ BCrypt.Net-Next available for password hashing
- ✅ Middleware order correct (Auth before Authz)

#### CatalogService (Port 5002)
- ✅ Listens on http://localhost:5002
- ✅ CatalogDbContext registered with DbContextOptions
- ✅ DonorsRepository registered as scoped
- ✅ GiftsRepository registered as scoped
- ✅ CategoriesRepository registered as scoped
- ✅ DonorsService registered as scoped
- ✅ GiftsService registered as scoped
- ✅ CategoriesService registered as scoped
- ✅ JWT Bearer authentication configured
- ✅ CORS policy "AllowAll" configured
- ✅ Serilog logging with file rolling
- ✅ CorrelationIdMiddleware added
- ✅ GlobalExceptionMiddleware added
- ✅ Swagger UI configured
- ✅ Duplicate middleware removed

#### OrderService (Port 5003)
- ✅ Listens on http://localhost:5003
- ✅ OrderDbContext registered with DbContextOptions
- ✅ OrdersRepository registered as scoped
- ✅ OrdersService registered as scoped
- ✅ AuthServiceClient registered with HTTP client
- ✅ CatalogServiceClient registered with HTTP client
- ✅ Polly retry policy enabled (3 retries, exponential backoff)
- ✅ Polly circuit breaker enabled (opens after 5 failures)
- ✅ HTTP client timeout set to 5 seconds
- ✅ JWT Bearer authentication configured
- ✅ CORS policy "AllowAll" configured
- ✅ Serilog logging with file rolling
- ✅ CorrelationIdMiddleware added
- ✅ GlobalExceptionMiddleware added
- ✅ Swagger UI configured

#### LotteryService (Port 5004)
- ✅ Listens on http://localhost:5004
- ✅ LotteryDbContext registered with DbContextOptions
- ✅ LotteryRepository registered as scoped
- ✅ LotteryDrawService registered as scoped
- ✅ AuthServiceClient registered with HTTP client
- ✅ CatalogServiceClient registered with HTTP client
- ✅ Polly retry policy enabled (3 retries, exponential backoff)
- ✅ Polly circuit breaker enabled (opens after 5 failures)
- ✅ HTTP client timeout set to 5 seconds
- ✅ JWT Bearer authentication configured
- ✅ CORS policy "AllowAll" configured
- ✅ Serilog logging with file rolling
- ✅ CorrelationIdMiddleware added
- ✅ GlobalExceptionMiddleware added
- ✅ Swagger UI configured

### Configuration Files

#### appsettings.json (All Services)
- ✅ ConnectionStrings.DefaultConnection configured
- ✅ Jwt.SecretKey = 32+ characters (same for all services)
- ✅ Jwt.Issuer = "AuthService"
- ✅ Jwt.ExpiryMinutes = 60
- ✅ ServiceUrls section present with all 4 service URLs

#### OrderService & LotteryService appsettings.json
- ✅ Services.AuthService.Url configured
- ✅ Services.CatalogService.Url configured

#### appsettings.Development.json (All Services)
- ✅ Logging.LogLevel.Default = Debug
- ✅ Service-specific logging level = Debug
- ✅ Polly logging enabled (for OrderService & LotteryService)

### NuGet Packages

#### All Services
- ✅ Swashbuckle.AspNetCore 6.4.0
- ✅ Microsoft.EntityFrameworkCore 9.0.0
- ✅ Microsoft.EntityFrameworkCore.SqlServer 9.0.0
- ✅ Microsoft.EntityFrameworkCore.Tools 9.0.0
- ✅ Microsoft.EntityFrameworkCore.Design 9.0.0
- ✅ Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11
- ✅ Serilog.AspNetCore 8.0.0
- ✅ Serilog.Sinks.File 5.0.0
- ✅ Polly 8.3.1
- ✅ Microsoft.Extensions.Http 9.0.0

#### AuthService Only
- ✅ BCrypt.Net-Next 4.2.0

#### OrderService & LotteryService
- ✅ Polly.Extensions.Http 3.0.0

### Middleware Pipeline Order

All Services (Consistent):
1. ✅ CorrelationIdMiddleware
2. ✅ GlobalExceptionMiddleware
3. ✅ Serilog Request Logging
4. ✅ HTTPS Redirection
5. ✅ CORS (AllowAll)
6. ✅ Authentication
7. ✅ Authorization
8. ✅ Map Controllers
9. ✅ Swagger (Development only)

### Database Configuration

- ✅ All services use same connection string
- ✅ Server: localhost\MSSQLSERVER01
- ✅ Database: Mechira-sinit-microservices
- ✅ Integrated Security: true
- ✅ TrustServerCertificate: true
- ✅ Encrypt: false
- ✅ Command timeout: 30 seconds

### JWT Configuration

- ✅ SecretKey: Same for all services
- ✅ Issuer: "AuthService"
- ✅ Audience: "MicroservicesApi"
- ✅ ExpiryMinutes: 60
- ✅ Clock skew: 5 seconds
- ✅ ValidateLifetime: true

### HTTP Clients (OrderService & LotteryService)

- ✅ AuthServiceClient base URL: http://localhost:5001
- ✅ CatalogServiceClient base URL: http://localhost:5002
- ✅ Accept header: application/json
- ✅ Timeout: 5 seconds
- ✅ Retry policy: 3 retries with exponential backoff (2s, 4s, 8s)
- ✅ Circuit breaker: 5 failures, 30 second open window

---

## Quick Start - Testing Phase 8

### 1. Build All Services
```bash
cd d:\Documents\שרי\לימודים\microservise\server\Services\AuthService
dotnet build

cd ..\CatalogService
dotnet build

cd ..\OrderService
dotnet build

cd ..\LotteryService
dotnet build
```

Expected output: `Build succeeded`

### 2. Verify Dependency Injection

#### AuthService
```bash
cd d:\Documents\שרי\לימודים\microservise\server\Services\AuthService
dotnet run
# Should show: "Now listening on: http://localhost:5001"
# Press Ctrl+C to stop
```

#### CatalogService
```bash
cd d:\Documents\שרי\לימודים\microservise\server\Services\CatalogService
dotnet run
# Should show: "Now listening on: http://localhost:5002"
# Press Ctrl+C to stop
```

#### OrderService
```bash
cd d:\Documents\שרי\לימודים\microservise\server\Services\OrderService
dotnet run
# Should show: "Now listening on: http://localhost:5003"
# Should show successful HTTP client registrations
# Press Ctrl+C to stop
```

#### LotteryService
```bash
cd d:\Documents\שרי\לימודים\microservise\server\Services\LotteryService
dotnet run
# Should show: "Now listening on: http://localhost:5004"
# Should show successful HTTP client registrations
# Press Ctrl+C to stop
```

### 3. Access Swagger UI

```bash
# AuthService Swagger
http://localhost:5001/swagger

# CatalogService Swagger
http://localhost:5002/swagger

# OrderService Swagger
http://localhost:5003/swagger

# LotteryService Swagger
http://localhost:5004/swagger
```

### 4. Check Logs

```powershell
# AuthService logs
Get-Content "d:\Documents\שרי\לימודים\microservise\server\Services\AuthService\logs\auth-service-*.txt" -Tail 20

# CatalogService logs
Get-Content "d:\Documents\שרי\לימודים\microservise\server\Services\CatalogService\logs\catalog-service-*.txt" -Tail 20

# OrderService logs
Get-Content "d:\Documents\שרי\לימודים\microservise\server\Services\OrderService\logs\order-service-*.txt" -Tail 20

# LotteryService logs
Get-Content "d:\Documents\שרי\לימודים\microservise\server\Services\LotteryService\logs\lottery-service-*.txt" -Tail 20
```

### 5. Test Database Connection

```bash
# Create a simple endpoint call (requires service running)
curl http://localhost:5001/api/users

# Should either return data or appropriate error
# Check logs for database connection messages
```

### 6. Test HTTP Client Resilience (OrderService)

```bash
# Stop CatalogService (Ctrl+C)

# Try to call OrderService
curl -H "Authorization: Bearer TOKEN" \
  http://localhost:5003/api/orders

# Check OrderService logs for:
# - Retry attempts
# - Circuit breaker activation
# - Fallback behavior
```

---

## Common Issues & Solutions

### Issue: Build fails with package errors
**Solution**:
```bash
dotnet clean
dotnet restore
dotnet build
```

### Issue: "Port already in use" error
**Solution**:
```powershell
# Find process using port
netstat -ano | findstr :5001

# Kill the process
taskkill /PID 1234 /F
```

### Issue: DI container resolution error
**Check**:
- All repositories have `AddScoped<IXxx, Xxx>()`
- All services have `AddScoped<IXxx, Xxx>()`
- Middleware order is correct

### Issue: Database connection timeout
**Check**:
- SQL Server is running
- Connection string is correct
- Database name exists or migrations need to run

### Issue: Swagger UI not loading
**Check**:
- Service is running on correct port
- Browser URL is correct
- Swagger configuration in Program.cs

### Issue: HTTP client timeout in OrderService
**Check**:
- Target service (AuthService/CatalogService) is running
- Service URL in appsettings.json is correct
- Network connectivity
- Firewall rules

---

## Configuration Template for New Service

If adding a new service (e.g., PaymentService on port 5005):

```csharp
// Program.cs template
using Serilog;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Repository;
using PaymentService.Services;
using PaymentService.Interfaces;
using PaymentService.Middleware;
using SharedModels.Utilities;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog
builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File("logs/payment-service-.txt", rollingInterval: RollingInterval.Day)
        .Enrich.FromLogContext();
});

// 2. Database
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// 3. DI - Add your repositories and services
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// 4. HTTP Clients (if needed)
builder.Services.AddHttpClient<AuthServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:AuthService:Url"] ?? "http://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(5);
})
    .AddPolicyHandler(PollyPoliciesExtension.GetRetryPolicy())
    .AddPolicyHandler(PollyPoliciesExtension.GetCircuitBreakerPolicy());

// 5. CORS, Auth, Swagger...
builder.Services.AddCors(options => /* ... */);
builder.Services.AddAuthentication("Bearer").AddJwtBearer(/* ... */);
builder.Services.AddSwaggerGen(/* ... */);
builder.Services.AddControllers();

var app = builder.Build();

// Middleware pipeline...
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run("http://localhost:5005");
```

---

## Summary

✅ **Phase 8 Complete**

All services are configured with:
- Proper DI containers
- Entity Framework setup
- Serilog logging
- JWT authentication
- HTTP clients (OrderService, LotteryService)
- Polly resilience policies
- Swagger documentation
- Correct middleware pipeline

**Ready for Phase 9: Database Migrations**

---

## Related Files

- [PHASE8_PROGRAM_CONFIGURATION.md](./PHASE8_PROGRAM_CONFIGURATION.md) - Detailed guide
- [PHASE8_CONFIGURATION_EXAMPLES.md](./PHASE8_CONFIGURATION_EXAMPLES.md) - Code examples
- [QUICK_START_GUIDE.md](./QUICK_START_GUIDE.md) - Running the system

---

**Last Updated**: Phase 8  
**Status**: ✅ Ready for Migrations  
**Date**: 2024
