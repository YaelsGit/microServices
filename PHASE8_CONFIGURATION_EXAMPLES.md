# Phase 8 Configuration Examples

## Example 1: AuthService Program.cs (Complete)

```csharp
using Serilog;
using Microsoft.EntityFrameworkCore;
using AuthService.Data;
using AuthService.Repository;
using AuthService.Services;
using AuthService.Interfaces;
using SharedModels.Interfaces;
using AuthService.Middleware;
using SharedModels.Utilities;

var builder = WebApplication.CreateBuilder(args);

// ============ SERILOG CONFIGURATION ============
builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File(
            "logs/auth-service-.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level}] {Message:lj}{NewLine}{Exception}"
        )
        .Enrich.FromLogContext();
});

// ============ DATABASE CONFIGURATION ============
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(30)
    )
);

// ============ DEPENDENCY INJECTION ============
// Repositories
builder.Services.AddScoped<IUsersRepository, UsersRepository>();

// Services
builder.Services.AddScoped<IAuthService, UsersService>();
builder.Services.AddScoped<IUsersService, UsersService>();

// ============ CORS CONFIGURATION ============
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });

    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ============ AUTHENTICATION & AUTHORIZATION ============
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "your-super-secret-key-that-must-be-at-least-32-characters-long-for-hmac256";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AuthService";
var jwtExpiryMinutes = int.Parse(builder.Configuration["Jwt:ExpiryMinutes"] ?? "60");

// Utilities - Register JWT service after getting config values
builder.Services.AddSingleton(new JwtTokenService(jwtSecretKey, jwtIssuer, jwtExpiryMinutes));

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtSecretKey)
            ),
            ValidateIssuer = true,
            ValidIssuer = "AuthService",
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5)
        };
    });

builder.Services.AddAuthorization();

// ============ API DOCUMENTATION ============
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AuthService API",
        Version = "v1",
        Description = "Authentication and User Management Service"
    });

    // Add JWT Bearer authentication scheme to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// ============ CONTROLLERS ============
builder.Services.AddControllers();

var app = builder.Build();

// ============ MIDDLEWARE PIPELINE ============
// Correlation ID middleware (must be first)
app.UseMiddleware<CorrelationIdMiddleware>();

// Global exception handling middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// Logging
app.UseSerilogRequestLogging();

// HTTPS redirection
app.UseHttpsRedirection();

// CORS
app.UseCors("AllowAll");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Swagger/OpenAPI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthService API v1");
        options.RoutePrefix = "swagger";
    });
}

// Map controllers
app.MapControllers();

app.Run("http://localhost:5001");
```

---

## Example 2: OrderService Program.cs (With HTTP Clients & Polly)

```csharp
using Serilog;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Repository;
using OrderService.Services;
using OrderService.Interfaces;
using OrderService.Middleware;
using OrderService.Extensions;
using OrderService.HttpClients;
using SharedModels.Utilities;
using Polly.Extensions.Http;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ============ SERILOG CONFIGURATION ============
builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File(
            "logs/order-service-.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level}] {Message:lj}{NewLine}{Exception}"
        )
        .Enrich.FromLogContext();
});

// ============ DATABASE CONFIGURATION ============
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(30)
    )
);

// ============ DEPENDENCY INJECTION ============
// Repositories
builder.Services.AddScoped<IOrdersRepository, OrdersRepository>();

// Services
builder.Services.AddScoped<IOrdersService, OrdersService>();

// Utilities
builder.Services.AddSingleton<JwtTokenService>();

// ============ HTTP CLIENT CONFIGURATION WITH POLLY RESILIENCE ============
// AuthService HTTP Client with Polly resilience policies
builder.Services.AddHttpClient<AuthServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:AuthService:Url"] ?? "http://localhost:5001");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(5);
})
    .AddPolicyHandler(PollyPoliciesExtension.GetRetryPolicy())
    .AddPolicyHandler(PollyPoliciesExtension.GetCircuitBreakerPolicy());

// CatalogService HTTP Client with Polly resilience policies
builder.Services.AddHttpClient<CatalogServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:CatalogService:Url"] ?? "http://localhost:5002");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(5);
})
    .AddPolicyHandler(PollyPoliciesExtension.GetRetryPolicy())
    .AddPolicyHandler(PollyPoliciesExtension.GetCircuitBreakerPolicy());

// ============ CORS CONFIGURATION ============
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });

    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ============ AUTHENTICATION & AUTHORIZATION ============
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "your-secret-key-min-32-characters-required-here";

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtSecretKey)
            ),
            ValidateIssuer = true,
            ValidIssuer = "AuthService",
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5)
        };
    });

builder.Services.AddAuthorization();

// ============ API DOCUMENTATION ============
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OrderService API",
        Version = "v1",
        Description = "Order Management Service with inter-service resilience"
    });

    // Add JWT Bearer authentication scheme to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// ============ CONTROLLERS ============
builder.Services.AddControllers();

var app = builder.Build();

// ============ MIDDLEWARE PIPELINE ============
// Correlation ID middleware (must be first)
app.UseMiddleware<CorrelationIdMiddleware>();

// Global exception handling middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// Logging
app.UseSerilogRequestLogging();

// HTTPS redirection
app.UseHttpsRedirection();

// CORS
app.UseCors("AllowAll");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Swagger/OpenAPI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "OrderService API v1");
        options.RoutePrefix = "swagger";
    });
}

// Map controllers
app.MapControllers();

app.Run("http://localhost:5003");
```

---

## Example 3: Polly Extension Configuration

Location: `OrderService/Extensions/PollyPoliciesExtension.cs`

```csharp
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Extensions.Http;

namespace OrderService.Extensions
{
    public static class PollyPoliciesExtension
    {
        /// <summary>
        /// Creates a retry policy for HTTP requests
        /// Retries up to 3 times with exponential backoff
        /// Only retries on transient failures
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2s, 4s, 8s
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds} seconds");
                    });
        }

        /// <summary>
        /// Creates a circuit breaker policy for HTTP requests
        /// Opens after 5 consecutive failures
        /// Stays open for 30 seconds before trying again
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (outcome, timespan) =>
                    {
                        Console.WriteLine($"Circuit breaker opened for {timespan.TotalSeconds} seconds");
                    },
                    onReset: () =>
                    {
                        Console.WriteLine("Circuit breaker reset");
                    });
        }
    }
}
```

---

## Example 4: HttpClient Implementation

Location: `OrderService/HttpClients/AuthServiceClient.cs`

```csharp
using System.Text.Json;
using SharedModels.Dtos;

namespace OrderService.HttpClients
{
    public class AuthServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthServiceClient> _logger;

        public AuthServiceClient(HttpClient httpClient, ILogger<AuthServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Validates a user token with the Auth Service
        /// </summary>
        public async Task<UserDto?> ValidateTokenAsync(string token)
        {
            try
            {
                _logger.LogInformation("Validating token with AuthService");

                var response = await _httpClient.GetAsync("/api/users/validate");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Token validation failed: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<UserDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("Token validated successfully");
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                throw; // Polly will handle the retry
            }
        }
    }
}
```

---

## Example 5: Ploidy Extension in LotteryService

Location: `LotteryService/Extensions/PollyPoliciesExtension.cs`

Same as OrderService - can be reused!

---

## Configuration File Hierarchy

### appsettings.json (All Services)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=Mechira-sinit-microservices;..."
  },
  "Jwt": {
    "SecretKey": "same-32-char-key-for-all-services",
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

### appsettings.Development.json (All Services)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Debug",
      "Microsoft.AspNetCore": "Debug",
      "[ServiceName]": "Debug",
      "Polly": "Debug"
    }
  }
}
```

### OrderService/LotteryService appsettings.json (Additional)
```json
{
  "Services": {
    "AuthService": {
      "Url": "http://localhost:5001"
    },
    "CatalogService": {
      "Url": "http://localhost:5002"
    }
  }
}
```

---

## Summary

- ✅ All services have consistent Program.cs structure
- ✅ DI containers fully configured
- ✅ Entity Framework ready
- ✅ Serilog logging integrated
- ✅ CORS enabled for Angular
- ✅ JWT authentication configured
- ✅ OrderService & LotteryService have HTTP clients with Polly
- ✅ Swagger UI available for all services
- ✅ Middleware pipeline in correct order

All services are ready for database migrations!
