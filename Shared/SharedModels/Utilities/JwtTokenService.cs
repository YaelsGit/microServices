using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace SharedModels.Utilities;

public class JwtTokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly int _expiryMinutes;

    public JwtTokenService(string secretKey, string issuer = "AuthService", int expiryMinutes = 60)
    {
        _secretKey = secretKey;
        _issuer = issuer;
        _expiryMinutes = expiryMinutes;
    }

    public string GenerateToken(int userId, string email, string firstName, string lastName, string role = "user")
    {
        var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.GivenName, firstName),
            new Claim(ClaimTypes.Surname, lastName),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: "MicroservicesApi",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (bool IsValid, int UserId, string Email, string Role) ValidateToken(string token)
    {
        try
        {
            var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_secretKey));
            var handler = new JwtSecurityTokenHandler();

            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = false,
                ValidateLifetime = true
            }, out SecurityToken validatedToken);

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            var emailClaim = principal.FindFirst(ClaimTypes.Email);
            var roleClaim = principal.FindFirst(ClaimTypes.Role);

            if (userIdClaim == null || emailClaim == null)
                return (false, 0, string.Empty, string.Empty);

            if (int.TryParse(userIdClaim.Value, out int userId))
            {
                return (true, userId, emailClaim.Value, roleClaim?.Value ?? "user");
            }

            return (false, 0, string.Empty, string.Empty);
        }
        catch
        {
            return (false, 0, string.Empty, string.Empty);
        }
    }

    public DateTime GetTokenExpiry(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
            return jwtToken?.ValidTo ?? DateTime.MinValue;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}
