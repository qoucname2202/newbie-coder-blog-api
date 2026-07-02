using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace NewbieCoder.API.Middlewares;

/// <summary>
/// Validates the JWT Bearer token on incoming requests and populates HttpContext.User.
/// Must be registered after UseRouting and before the authorization middleware / controllers.
/// </summary>
public sealed class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JwtMiddlewareSettings _settings;
    private readonly JwtSecurityTokenHandler _handler = new();

    public AuthMiddleware(RequestDelegate next, JwtMiddlewareSettings settings)
    {
        _next = next;
        _settings = settings;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(authHeader) &&
            authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();

            if (!string.IsNullOrEmpty(token))
            {
                var principal = TryValidateToken(token);
                if (principal != null)
                {
                    var authService = context.RequestServices.GetRequiredService<Core.Interfaces.Services.IAuthService>();
                    if (!authService.IsTokenRevoked(token))
                    {
                        context.User = principal;
                    }
                }
            }
        }

        await _next(context);
    }

    private ClaimsPrincipal? TryValidateToken(string token)
    {
        try
        {
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret)),
                ClockSkew = TimeSpan.Zero
            };

            return _handler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// JWT middleware configuration — read from appsettings.json under JwtSettings.
/// </summary>
public sealed class JwtMiddlewareSettings
{
    public const string SectionName = "JwtSettings";

    public required string Secret { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
}
