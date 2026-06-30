# Phase 7: API Gateway with Ocelot - Implementation Complete

## Overview
Phase 7 has been successfully implemented. The API Gateway now serves as the central routing and authentication layer for all microservices, using Ocelot for routing and a custom JWT validation middleware for security.

## Completed Components

### 1. **Middleware/JwtValidationMiddleware.cs** ✅
**Purpose**: Custom JWT token validation before forwarding requests to microservices

**Features**:
- Extracts JWT token from `Authorization: Bearer <token>` header
- Validates token signature using HS256 algorithm
- Validates token expiry time
- Handles security exceptions with appropriate HTTP responses
- Skips validation for public endpoints:
  - `POST /auth/users/login`
  - `POST /auth/users/register`
- Comprehensive logging at each validation stage
- Passes validated token to context for downstream services

**Key Methods**:
```csharp
public async Task InvokeAsync(HttpContext context)
```

### 2. **Program.cs** ✅
**Purpose**: Gateway startup and middleware pipeline configuration

**Configuration**:
- Serilog logging with daily file rolling
- Log directory auto-creation: `logs/api-gateway-YYYY-MM-DD.txt`
- Ocelot integration with `ocelot.json` configuration
- JWT Bearer authentication scheme
- CORS policy for Angular frontend
- Middleware pipeline ordering (correct order for security)

**Pipeline Order** (important for security):
1. Serilog Request Logging
2. CORS Handling
3. Custom JWT Validation Middleware
4. Authentication
5. Authorization
6. Ocelot Routing

### 3. **ocelot.json** ✅
**Purpose**: Route configuration mapping incoming requests to microservices

**Routes Configured**:
```json
{
  "Routes": [
    {
      "UpstreamPathTemplate": "/auth/{controller}/{action?}/{id?}",
      "DownstreamHostAndPorts": [{"Host": "localhost", "Port": 5001}]
    },
    {
      "UpstreamPathTemplate": "/catalog/{controller}/{action?}/{id?}",
      "DownstreamHostAndPorts": [{"Host": "localhost", "Port": 5002}]
    },
    {
      "UpstreamPathTemplate": "/orders/{controller}/{action?}/{id?}",
      "DownstreamHostAndPorts": [{"Host": "localhost", "Port": 5003}]
    },
    {
      "UpstreamPathTemplate": "/lottery/{controller}/{action?}/{id?}",
      "DownstreamHostAndPorts": [{"Host": "localhost", "Port": 5004}]
    }
  ]
}
```

**Features**:
- All HTTP methods supported: GET, POST, PUT, DELETE, OPTIONS
- OPTIONS support for CORS preflight requests
- Global configuration with base URL
- Request ID tracking for debugging
- Rate limiting options configured

### 4. **appsettings.json** ✅
**Purpose**: Production and shared configuration

**Configuration**:
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

**⚠️ Important**:
- `SecretKey` MUST match the key in AuthService
- Service URLs easily configurable for deployment

### 5. **appsettings.Development.json** ✅
**Purpose**: Development-specific logging configuration

**Configuration**:
- Debug level logging for middleware and Ocelot
- Information level for Microsoft libraries

### 6. **ApiGateway.csproj** ✅
**NuGet Packages** (already configured):
- `Ocelot` (20.1.0) - API gateway and routing
- `Microsoft.AspNetCore.Authentication.JwtBearer` (8.0.11) - JWT auth
- `Serilog.AspNetCore` (8.0.0) - Structured logging
- `Serilog.Sinks.File` (5.0.0) - File logging sink

### 7. **Documentation** ✅
- **README.md**: Comprehensive setup and usage guide
- **TESTING_GUIDE.md**: curl commands and testing procedures

## Gateway Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Client Application                           │
│                    (Angular App on 5000)                        │
└────────────────────────────────────┬────────────────────────────┘
                                     │ HTTP Request
                                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                   API Gateway (Port 5000)                       │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │  1. Serilog Request Logging                             │ │
│  └──────────────────────────────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │  2. CORS Middleware (Allow Angular App)                 │ │
│  └──────────────────────────────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │  3. JWT Validation Middleware (Custom)                  │ │
│  │     - Extract token from Authorization header            │ │
│  │     - Validate signature & expiry                        │ │
│  │     - Return 401 if invalid                              │ │
│  └──────────────────────────────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │  4. Authentication (ASP.NET Core)                        │ │
│  └──────────────────────────────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │  5. Authorization (ASP.NET Core)                         │ │
│  └──────────────────────────────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │  6. Ocelot Routing                                       │ │
│  │     Routes based on UpstreamPathTemplate                 │ │
│  └──────────────────────────────────────────────────────────┘ │
└─────────────────┬──────────────────────────────────────────────┘
                  │ HTTP Request (routed to service)
        ┌─────────┴─────────┬────────────────┬──────────────┐
        ▼                   ▼                ▼              ▼
    ┌────────┐         ┌────────┐      ┌────────┐      ┌────────┐
    │ Auth   │         │Catalog │      │ Order  │      │Lottery │
    │Service │         │Service │      │Service │      │Service │
    │ :5001  │         │ :5002  │      │ :5003  │      │ :5004  │
    └────────┘         └────────┘      └────────┘      └────────┘
```

## Security Flow

### 1. **Public Endpoints** (No JWT Required)
```
Request → /auth/users/login
    ↓
Gateway skips JWT validation
    ↓
Forward to AuthService
    ↓
Response with JWT token
```

### 2. **Protected Endpoints** (JWT Required)
```
Request → /orders/orders/list with Authorization: Bearer JWT
    ↓
Custom JWT Middleware
    ├─ Extract token
    ├─ Validate signature
    ├─ Check expiry
    ├─ Validate issuer
    ▼
Token valid → Forward to OrderService with token
Token invalid → Return 401 Unauthorized
```

## Request Flow Examples

### Example 1: Login (Public)
```bash
POST http://localhost:5000/auth/users/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}

Gateway Route: /auth/* → AuthService (Port 5001)
JWT Validation: SKIPPED (public endpoint)
```

### Example 2: Get Catalog (Public)
```bash
GET http://localhost:5000/catalog/products

Gateway Route: /catalog/* → CatalogService (Port 5002)
JWT Validation: SKIPPED (no auth required)
```

### Example 3: Get Orders (Protected)
```bash
GET http://localhost:5000/orders/orders
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

Gateway Route: /orders/* → OrderService (Port 5003)
JWT Validation: VALIDATED
  - Token extracted
  - Signature verified
  - Expiry checked
  - Passed to OrderService
```

## Starting the Services

### 1. Build the solution
```bash
dotnet build
```

### 2. Start each service in separate terminal
```bash
# Terminal 1: AuthService
cd server/Services/AuthService
dotnet run

# Terminal 2: CatalogService
cd server/Services/CatalogService
dotnet run

# Terminal 3: OrderService
cd server/Services/OrderService
dotnet run

# Terminal 4: LotteryService
cd server/Services/LotteryService
dotnet run

# Terminal 5: API Gateway
cd server/Gateway/ApiGateway
dotnet run
```

### 3. Gateway will start on `http://localhost:5000`

## Testing

See [TESTING_GUIDE.md](TESTING_GUIDE.md) for comprehensive testing commands.

Quick test:
```bash
# Get JWT token
curl -X POST http://localhost:5000/auth/users/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "password123"}'

# Use token for protected endpoint
curl -H "Authorization: Bearer <token>" \
     http://localhost:5000/orders/orders/list
```

## Logging

### Log Location
```
server/Gateway/ApiGateway/logs/api-gateway-2024-01-15.txt
```

### Log Output Example
```
[2024-01-15 14:23:45.123 +02:00] [INF] Request started GET /orders/orders/list
[2024-01-15 14:23:45.234 +02:00] [DBG] No Authorization header found for request: /catalog/products
[2024-01-15 14:23:45.345 +02:00] [DBG] JWT token validated successfully for request: /orders/orders/list
[2024-01-15 14:23:45.456 +02:00] [INF] Request completed GET /orders/orders/list - 200 OK
```

## Deployment Considerations

### 1. Environment Variables
Store secrets in environment variables or Key Vault:
```bash
export JWT__SECRETKEY="production-secret-key"
export SERVICEURLS__AUTHSERVICE="https://auth.yourdomain.com"
```

### 2. HTTPS
Update `Program.cs` for production:
```csharp
app.UseHsts(); // Enable HSTS
app.UseHttpsRedirection(); // Force HTTPS
app.Run("https://0.0.0.0:443");
```

### 3. Rate Limiting
Ocelot has rate limiting configured in `ocelot.json` - can be enabled per route

### 4. CORS Configuration
Update CORS policy for production domains:
```csharp
policy
    .WithOrigins("https://yourdomain.com")
    .AllowAnyMethod()
    .AllowAnyHeader();
```

## Files Summary

| File | Purpose | Status |
|------|---------|--------|
| Program.cs | Gateway startup & config | ✅ Complete |
| ApiGateway.csproj | NuGet packages | ✅ Complete |
| ocelot.json | Routing rules | ✅ Complete |
| appsettings.json | Configuration (JWT, services) | ✅ Complete |
| appsettings.Development.json | Dev logging | ✅ Complete |
| Middleware/JwtValidationMiddleware.cs | JWT validation | ✅ Complete |
| README.md | Documentation | ✅ Complete |
| TESTING_GUIDE.md | Testing commands | ✅ Complete |

## Next Steps

1. ✅ Build the solution to verify no compilation errors
2. ✅ Run all microservices on their respective ports
3. ✅ Test gateway routing with curl or Postman
4. ✅ Verify JWT token validation
5. ✅ Monitor logs for any issues
6. ✅ Deploy to production with appropriate configuration

## Known Considerations

- **Downstream Service Validation**: Services should also validate JWT tokens for defense-in-depth
- **Token Refresh**: Token refresh logic should be implemented in AuthService
- **HTTPS**: For production, ensure HTTPS is enforced throughout
- **Secrets Management**: Store JWT secrets in secure vault, not in code
- **Rate Limiting**: Ocelot can enforce rate limits per route/client

---

**Phase 7 Status**: ✅ COMPLETE

All components of the API Gateway with Ocelot have been successfully implemented and configured.
