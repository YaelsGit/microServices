# Phase 8: Program.cs Configuration for Each Service

**Status**: ✅ **COMPLETE**

## Overview

Phase 8 has successfully configured dependency injection, Entity Framework, logging, and CORS for all microservices. Each service now has:

- ✅ Proper dependency injection setup
- ✅ Entity Framework DbContext registration
- ✅ Serilog logging with daily file rolling
- ✅ CORS policy for Angular frontend
- ✅ JWT Bearer authentication
- ✅ Swagger/OpenAPI documentation
- ✅ Correct middleware pipeline order
- ✅ Service-specific HTTP clients with Polly resilience (OrderService, LotteryService)

---

## Service Configuration Summary

### 1. AuthService (Port 5001)

**Program.cs Configuration**:
```csharp
// ✅ Database
DbContext: AuthDbContext
ConnectionString: SharedModels.Microservices database

// ✅ DI Repositories & Services
- IUsersRepository → UsersRepository
- IAuthService → UsersService
- IUsersService → UsersService

// ✅ Security
- JWT Bearer authentication
- JwtTokenService for token generation
- Uses BCrypt for password hashing

// ✅ Middleware Pipeline
1. CorrelationIdMiddleware
2. GlobalExceptionMiddleware
3. Serilog Request Logging
4. CORS (AllowAll policy)
5. Authentication
6. Authorization
7. MapControllers
```

**Key Features**:
- Listens on `http://localhost:5001`
- BCrypt.Net-Next v4.2.0 for password operations
- Polly v8.3.1 for resilience
- Swagger UI available at `/swagger`
- Daily log files in `logs/auth-service-YYYY-MM-DD.txt`

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\MSSQLSERVER01;Database=Mechira-sinit-microservices;..."
  },
  "Jwt": {
    "SecretKey": "your-super-secret-key-that-must-be-at-least-32-characters-long",
    "Issuer": "AuthService",
    "Audience": "MicroservicesApi",
    "ExpiryMinutes": 60
  },
  "ServiceUrls": {
    "AuthService": "http://localhost:5001",
    "CatalogService": "http://localhost:5002",
    "OrderService": "http://localhost:5003",
    "LotteryService": "http://localhost:5004"
  }
}
```

---

### 2. CatalogService (Port 5002)

**Program.cs Configuration**:
```csharp
// ✅ Database
DbContext: CatalogDbContext
ConnectionString: SharedModels.Microservices database

// ✅ DI Repositories & Services
- IDonorsRepository → DonorsRepository
- IGiftsRepository → GiftsRepository
- ICategoriesRepository → CategoriesRepository
- IDonorsService → DonorsService
- IGiftsService → GiftsService
- ICategoriesService → CategoriesService

// ✅ Security
- JWT Bearer authentication
- No HTTP clients needed

// ✅ Middleware Pipeline
1. CorrelationIdMiddleware
2. GlobalExceptionMiddleware
3. Serilog Request Logging
4. CORS (AllowAll policy)
5. Authentication
6. Authorization
7. MapControllers
```

**Key Features**:
- Listens on `http://localhost:5002`
- No external service dependencies
- Swagger UI available at `/swagger`
- Daily log files in `logs/catalog-service-YYYY-MM-DD.txt`

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\MSSQLSERVER01;Database=Mechira-sinit-microservices;..."
  },
  "Jwt": {
    "SecretKey": "your-super-secret-key-that-must-be-at-least-32-characters-long",
    "Issuer": "AuthService",
    "Audience": "MicroservicesApi",
    "ExpiryMinutes": 60
  },
  "ServiceUrls": {
    "AuthService": "http://localhost:5001",
    "CatalogService": "http://localhost:5002",
    "OrderService": "http://localhost:5003",
    "LotteryService": "http://localhost:5004"
  }
}
```

---

### 3. OrderService (Port 5003)

**Program.cs Configuration**:
```csharp
// ✅ Database
DbContext: OrderDbContext
ConnectionString: SharedModels.Microservices database

// ✅ DI Repositories & Services
- IOrdersRepository → OrdersRepository
- IOrdersService → OrdersService

// ✅ HTTP Clients with Polly Resilience
- AuthServiceClient
  * Base URL: http://localhost:5001
  * Retry Policy: 3 retries with exponential backoff
  * Circuit Breaker: Opens after 5 consecutive failures
  * Timeout: 5 seconds

- CatalogServiceClient
  * Base URL: http://localhost:5002
  * Retry Policy: 3 retries with exponential backoff
  * Circuit Breaker: Opens after 5 consecutive failures
  * Timeout: 5 seconds

// ✅ Security
- JWT Bearer authentication

// ✅ Middleware Pipeline
1. CorrelationIdMiddleware
2. GlobalExceptionMiddleware
3. Serilog Request Logging
4. CORS (AllowAll policy)
5. Authentication
6. Authorization
7. MapControllers
```

**Key Features**:
- Listens on `http://localhost:5003`
- Calls AuthService and CatalogService
- Polly policies handle transient failures
- Swagger UI available at `/swagger`
- Daily log files in `logs/order-service-YYYY-MM-DD.txt`

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\MSSQLSERVER01;Database=Mechira-sinit-microservices;..."
  },
  "Jwt": {
    "SecretKey": "your-super-secret-key-that-must-be-at-least-32-characters-long",
    "Issuer": "AuthService",
    "Audience": "MicroservicesApi",
    "ExpiryMinutes": 60
  },
  "Services": {
    "AuthService": {
      "Url": "http://localhost:5001"
    },
    "CatalogService": {
      "Url": "http://localhost:5002"
    }
  },
  "ServiceUrls": {
    "AuthService": "http://localhost:5001",
    "CatalogService": "http://localhost:5002",
    "OrderService": "http://localhost:5003",
    "LotteryService": "http://localhost:5004"
  }
}
```

---

### 4. LotteryService (Port 5004)

**Program.cs Configuration**:
```csharp
// ✅ Database
DbContext: LotteryDbContext
ConnectionString: SharedModels.Microservices database

// ✅ DI Repositories & Services
- ILotteryRepository → LotteryRepository
- ILotteryService → LotteryDrawService

// ✅ HTTP Clients with Polly Resilience
- AuthServiceClient
  * Base URL: http://localhost:5001
  * Retry Policy: 3 retries with exponential backoff
  * Circuit Breaker: Opens after 5 consecutive failures
  * Timeout: 5 seconds

- CatalogServiceClient
  * Base URL: http://localhost:5002
  * Retry Policy: 3 retries with exponential backoff
  * Circuit Breaker: Opens after 5 consecutive failures
  * Timeout: 5 seconds

// ✅ Security
- JWT Bearer authentication

// ✅ Middleware Pipeline
1. CorrelationIdMiddleware
2. GlobalExceptionMiddleware
3. Serilog Request Logging
4. CORS (AllowAll policy)
5. Authentication
6. Authorization
7. MapControllers
```

**Key Features**:
- Listens on `http://localhost:5004`
- Calls AuthService and CatalogService
- Polly policies handle transient failures
- Swagger UI available at `/swagger`
- Daily log files in `logs/lottery-service-YYYY-MM-DD.txt`

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\MSSQLSERVER01;Database=Mechira-sinit-microservices;..."
  },
  "Jwt": {
    "SecretKey": "your-super-secret-key-that-must-be-at-least-32-characters-long",
    "Issuer": "AuthService",
    "Audience": "MicroservicesApi",
    "ExpiryMinutes": 60
  },
  "Services": {
    "AuthService": {
      "Url": "http://localhost:5001"
    },
    "CatalogService": {
      "Url": "http://localhost:5002"
    }
  },
  "ServiceUrls": {
    "AuthService": "http://localhost:5001",
    "CatalogService": "http://localhost:5002",
    "OrderService": "http://localhost:5003",
    "LotteryService": "http://localhost:5004"
  }
}
```

---

## NuGet Packages Installed

### All Services
- ✅ Swashbuckle.AspNetCore 6.4.0 (Swagger)
- ✅ Microsoft.EntityFrameworkCore 9.0.0
- ✅ Microsoft.EntityFrameworkCore.SqlServer 9.0.0
- ✅ Microsoft.EntityFrameworkCore.Tools 9.0.0
- ✅ Microsoft.EntityFrameworkCore.Design 9.0.0
- ✅ Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11
- ✅ Serilog.AspNetCore 8.0.0
- ✅ Serilog.Sinks.File 5.0.0
- ✅ Polly 8.3.1
- ✅ Microsoft.Extensions.Http 9.0.0

### AuthService Only
- ✅ BCrypt.Net-Next 4.2.0 (Password hashing)

### OrderService & LotteryService
- ✅ Polly.Extensions.Http 3.0.0 (HTTP client resilience)

---

## Middleware Pipeline Order (Critical for Security)

All services follow this consistent middleware order:

1. **CorrelationIdMiddleware** - Tracks requests across services
2. **GlobalExceptionMiddleware** - Centralized error handling
3. **Serilog Request Logging** - Logs all incoming requests
4. **HTTPS Redirection** - Enforces HTTPS
5. **CORS** - Allows cross-origin requests from Angular
6. **Authentication** - JWT token validation
7. **Authorization** - Role-based access control
8. **Route Mapping** - Maps controllers/endpoints
9. **Swagger UI** (Development only) - API documentation

**Important**: Authentication must come before Authorization!

---

## Key Configuration Details

### Connection String
All services share a single database:
```
Server=localhost\MSSQLSERVER01;
Database=Mechira-sinit-microservices;
Integrated Security=true;
TrustServerCertificate=true;
Encrypt=false;
```

**Note**: Update server name and database name for your environment.

### JWT Configuration
- **SecretKey**: 32+ character key (same for all services)
- **Issuer**: "AuthService" (must match)
- **Audience**: "MicroservicesApi"
- **ExpiryMinutes**: 60 (1 hour)

**Critical**: Keep SecretKey synchronized across all services!

### Logging Levels

**Production** (appsettings.json):
- Default: Information
- Microsoft: Warning
- Microsoft.AspNetCore: Warning

**Development** (appsettings.Development.json):
- Default: Debug
- Microsoft: Debug
- Microsoft.AspNetCore: Debug
- Service-specific: Debug

---

## Service-to-Service Communication

### OrderService Calling AuthService & CatalogService

```csharp
// Example in OrderService
public class OrdersService
{
    private readonly AuthServiceClient _authClient;
    private readonly CatalogServiceClient _catalogClient;

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        // Verify catalog item exists
        var product = await _catalogClient.GetProductAsync(request.ProductId);
        
        if (product == null)
            throw new NotFoundException("Product not found");

        // Create order in database
        var order = new Order { /* ... */ };
        await _repo.AddAsync(order);
        return order;
    }
}
```

### Polly Resilience Policies

**Retry Policy**:
- Retries 3 times
- Exponential backoff (2s, 4s, 8s)
- Only on transient failures (408, 429, 500, 502, 503, 504)

**Circuit Breaker Policy**:
- Opens after 5 consecutive failures
- Stays open for 30 seconds
- Prevents cascading failures

---

## Configuration for Different Environments

### Development (localhost)
```json
{
  "Services": {
    "AuthService": { "Url": "http://localhost:5001" },
    "CatalogService": { "Url": "http://localhost:5002" }
  }
}
```

### Staging
```json
{
  "Services": {
    "AuthService": { "Url": "https://staging-auth.yourdomain.com" },
    "CatalogService": { "Url": "https://staging-catalog.yourdomain.com" }
  }
}
```

### Production
```json
{
  "Services": {
    "AuthService": { "Url": "https://auth.yourdomain.com" },
    "CatalogService": { "Url": "https://catalog.yourdomain.com" }
  }
}
```

---

## Verification Checklist

- ✅ Each service starts on correct port (5001-5004)
- ✅ DI containers configured with repositories and services
- ✅ Entity Framework DbContext registered
- ✅ Database connection string configured
- ✅ JWT authentication enabled
- ✅ CORS policy allows Angular frontend
- ✅ Serilog logging configured with file rolling
- ✅ Swagger UI available on `/swagger`
- ✅ OrderService and LotteryService have HTTP clients
- ✅ Polly policies enabled for resilience
- ✅ Middleware pipeline in correct order
- ✅ appsettings.json and appsettings.Development.json configured
- ✅ BCrypt available in AuthService

---

## Testing the Configuration

### Verify Dependency Injection
```bash
# Each service should start without DI errors
cd server/Services/AuthService
dotnet run
# Should show: "Now listening on: http://localhost:5001"
```

### Verify Database Connection
```bash
# Services should connect to database
# Check logs for connection success
cat logs/auth-service-2024-01-15.txt | grep -i "connected"
```

### Verify Swagger UI
```bash
# Access Swagger for each service
curl http://localhost:5001/swagger/v1/swagger.json  # AuthService
curl http://localhost:5002/swagger/v1/swagger.json  # CatalogService
curl http://localhost:5003/swagger/v1/swagger.json  # OrderService
curl http://localhost:5004/swagger/v1/swagger.json  # LotteryService
```

### Verify HTTP Client Resilience
```bash
# Stop AuthService and test OrderService
# Should retry 3 times then use circuit breaker
curl -H "Authorization: Bearer TOKEN" \
  http://localhost:5003/api/orders

# Check OrderService logs for Polly policy execution
cat logs/order-service-2024-01-15.txt | grep -i "retry\|circuit"
```

---

## Common Issues & Solutions

### Issue: Service won't start
**Error**: `The binding address already in use`
**Solution**: 
- Check port is not in use: `netstat -ano | findstr :5001`
- Kill process or use different port

### Issue: DI container resolution error
**Error**: `Unable to resolve service of type IXxxRepository`
**Solution**:
- Check AddScoped is called for all repositories
- Verify interfaces match implementation classes

### Issue: Database connection fails
**Error**: `A network-related or instance-specific error`
**Solution**:
- Verify SQL Server is running
- Check connection string in appsettings.json
- Verify database exists or create with migrations

### Issue: JWT validation fails
**Error**: `AuthenticationException: IDX10500: Signature validation failed`
**Solution**:
- Verify JWT:SecretKey matches in all services
- Check token hasn't expired
- Verify Issuer matches configuration

### Issue: HTTP client timeout in OrderService
**Error**: `The operation timed out`
**Solution**:
- Verify target service is running
- Check service URL in appsettings.json
- Review Polly policies in logs
- Increase timeout if needed

---

## Next Steps

1. ✅ Phase 8 Complete - Services configured
2. **Phase 9**: Database Migrations & Schema
3. **Phase 10**: Controllers & Endpoints
4. **Phase 11**: Integration Testing

---

## Summary

Phase 8 has successfully configured all services with:

- ✅ Proper dependency injection
- ✅ Entity Framework setup
- ✅ Serilog logging
- ✅ CORS for frontend
- ✅ JWT authentication
- ✅ Swagger documentation
- ✅ Correct middleware order
- ✅ Service-to-service HTTP clients
- ✅ Polly resilience policies
- ✅ Environment-specific configurations

All services are ready for database migrations and controller implementation in Phase 9!

---

**Date Completed**: Phase 8  
**Status**: ✅ Ready for Phase 9  
**Last Updated**: 2024
