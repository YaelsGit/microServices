using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ApiGateway.Middleware
{
    public class JwtValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtValidationMiddleware> _logger;
        private readonly string _secretKey;
        private readonly string _issuer;

        public JwtValidationMiddleware(RequestDelegate next, ILogger<JwtValidationMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _secretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT:SecretKey not configured");
            _issuer = configuration["Jwt:Issuer"] ?? "AuthService";
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip token validation for auth endpoints (login, register)
            if (context.Request.Path.StartsWithSegments("/auth/users/login", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/auth/users/register", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader))
            {
                // If no authorization header and request is not for auth endpoint, allow it through
                // (let the individual services handle auth requirements)
                _logger.LogInformation("No Authorization header found for request: {Path}", context.Request.Path);
                await _next(context);
                return;
            }

            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid Authorization header format for request: {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid authorization header format" });
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                // Validate the token signature and expiry
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = false, // Not validating audience
                    ValidateLifetime = true, // Validate token expiry
                    ClockSkew = TimeSpan.Zero // No clock skew tolerance
                }, out SecurityToken validatedToken);

                _logger.LogInformation("JWT token validated successfully for request: {Path}", context.Request.Path);

                // Attach the principal to the context for downstream services
                context.User = principal;

                // Pass the token to the request so downstream services can use it
                context.Items["jwt_token"] = token;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("JWT token expired for request: {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Token expired" });
                return;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                _logger.LogWarning("JWT token has invalid signature for request: {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid token signature" });
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("JWT token validation failed for request: {Path}. Error: {Error}", context.Request.Path, ex.Message);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid token" });
                return;
            }

            await _next(context);
        }
    }
}
