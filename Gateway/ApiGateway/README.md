# API Gateway - Ocelot Configuration

## Overview
This API Gateway serves as the central entry point for all client requests. It routes requests to appropriate microservices and validates JWT tokens before forwarding requests.

**Gateway Port**: `http://localhost:5000`

## Architecture

### Service Routes
- **Authentication Service**: `/auth/*` → `http://localhost:5001`
- **Catalog Service**: `/catalog/*` → `http://localhost:5002`
- **Order Service**: `/orders/*` → `http://localhost:5003`
- **Lottery Service**: `/lottery/*` → `http://localhost:5004`

## Features

### 1. Request Routing
- Routes incoming requests to correct microservices based on URL path
- Maintains consistent API contract: `/api/{controller}/{action?}/{id?}`
- Supports HTTP methods: GET, POST, PUT, DELETE, OPTIONS

### 2. JWT Token Validation
- Custom middleware extracts JWT token from `Authorization: Bearer <token>` header
- Validates token signature using shared secret key
- Validates token expiry
- Returns `401 Unauthorized` for invalid/expired tokens
- Passes validated token to downstream services for re-validation

### 3. Centralized Logging
- Serilog integration with daily file rolling logs
- Logs stored in `logs/` directory
- Structured logging with timestamps, levels, and request context
- Development mode: Debug level logging for troubleshooting

### 4. CORS Support
- Configured for Angular frontend
- Allows all origins, methods, and headers (configurable in `Program.cs`)

## Configuration Files

### `appsettings.json`
```json
{
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

**⚠️ IMPORTANT**: Ensure `Jwt:SecretKey` matches the secret key used in AuthService.

### `ocelot.json`
Defines routing rules for each microservice:
- Downstream path templates
- HTTP methods allowed
- Authentication requirements
- Request ID tracking

### `appsettings.Development.json`
Enables verbose logging for development:
- Debug level for gateway and middleware
- Information level for Microsoft libraries

## Middleware Pipeline

1. **Serilog Request Logging** - Logs all incoming requests
2. **CORS** - Handles cross-origin requests
3. **JWT Validation Middleware** - Custom validation of JWT tokens
4. **Authentication** - ASP.NET Core authentication scheme
5. **Authorization** - ASP.NET Core authorization
6. **Ocelot** - Routes request to appropriate microservice

## Running the Gateway

### Prerequisites
- .NET 8.0 SDK
- All microservices running on their respective ports (5001-5004)

### Start the Gateway
```bash
dotnet run
```

The gateway will start on `http://localhost:5000`

### Making Requests

#### Without Authentication (Catalog Service)
```bash
curl http://localhost:5000/catalog/products
```

#### With Authentication (Orders Service)
```bash
curl -H "Authorization: Bearer <jwt_token>" \
     http://localhost:5000/orders/orders
```

## Logging

Logs are stored in:
- `logs/api-gateway-YYYY-MM-DD.txt` - Daily rolling log files

Log format:
```
[2024-01-15 14:23:45.123 +02:00] [INF] Request started for /catalog/products
[2024-01-15 14:23:45.234 +02:00] [INF] JWT token validated successfully
[2024-01-15 14:23:45.345 +02:00] [INF] Request completed with status 200
```

## Deployment Configuration

For production deployment, update these values:

### `appsettings.json`
```json
{
  "ServiceUrls": {
    "AuthService": "https://auth-service.yourdomain.com",
    "CatalogService": "https://catalog-service.yourdomain.com",
    "OrderService": "https://order-service.yourdomain.com",
    "LotteryService": "https://lottery-service.yourdomain.com"
  }
}
```

### `Program.cs`
Update the host configuration:
```csharp
app.Run("http://0.0.0.0:80"); // Or your production port
```

## Troubleshooting

### 401 Unauthorized
- Verify JWT token is included in Authorization header
- Check token hasn't expired
- Ensure `Jwt:SecretKey` in gateway matches AuthService
- Check token issuer matches configured issuer

### 404 Not Found
- Verify service URL in `appsettings.json` is correct
- Ensure target service is running
- Check routing path in `ocelot.json`

### Service Connection Error
- Verify microservice is running on correct port
- Check firewall rules
- Review gateway logs for detailed error messages

## Files Structure

```
ApiGateway/
├── Program.cs                    # Gateway startup and configuration
├── ApiGateway.csproj             # Project file with NuGet packages
├── ocelot.json                   # Routing configuration
├── appsettings.json              # Settings (JWT secrets, service URLs)
├── appsettings.Development.json  # Development logging configuration
├── Middleware/
│   └── JwtValidationMiddleware.cs # Custom JWT validation
├── logs/                         # Generated log files (directory created on first run)
└── README.md                     # This file
```

## Security Considerations

1. **Secret Key Storage**: In production, store JWT secret key in Azure Key Vault or equivalent
2. **HTTPS Only**: Ensure HTTPS is enforced in production
3. **Rate Limiting**: Ocelot supports rate limiting (can be configured in `ocelot.json`)
4. **Token Refresh**: Implement token refresh logic in AuthService
5. **Audit Logging**: All token validations are logged for security auditing

## NuGet Dependencies

- **Ocelot** (20.1.0) - API Gateway and routing
- **Microsoft.AspNetCore.Authentication.JwtBearer** (8.0.11) - JWT authentication
- **Serilog.AspNetCore** (8.0.0) - Request logging
- **Serilog.Sinks.File** (5.0.0) - File logging sink

## Next Steps

1. Ensure all microservices are running
2. Test gateway routing with curl or Postman
3. Verify JWT token validation
4. Monitor logs for any issues
5. Deploy to production with appropriate configuration changes
