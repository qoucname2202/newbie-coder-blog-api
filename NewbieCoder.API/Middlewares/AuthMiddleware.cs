using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NewbieCoder.Core.Enums;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.API.Middlewares;

/// <summary>
/// Validates the JWT Bearer token on incoming requests and populates HttpContext.User.
/// Must be registered after UseRouting and before the authorization middleware / controllers.
/// Also checks if the authenticated user account has been locked and rejects the request if so.
/// </summary>
public sealed class AuthMiddleware(
    RequestDelegate next,
    JwtMiddlewareSettings settings,
    IServiceScopeFactory scopeFactory)
{
    private readonly JwtSecurityTokenHandler _handler = new();

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
                    // Check if account is locked before allowing the request to proceed
                    if (!await IsAccountLockedAsync(principal, context.RequestAborted))
                    {
                        context.User = principal;
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            responseStatus = new
                            {
                                responseCode = "00020405",
                                responseMessage = "Your account has been locked."
                            }
                        });
                        return;
                    }
                }
            }
        }

        await next(context);
    }

    private async Task<bool> IsAccountLockedAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
                      ?? principal.FindFirst(ClaimTypes.NameIdentifier);

        if (subClaim == null || !long.TryParse(subClaim.Value, out var userId))
            return false;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            return true;

        // Admin lock has no expiry — if status is Locked, reject the request.
        // Unlock is performed exclusively via the AdminUsersController endpoint.
        return user.Status == UserStatus.Locked;
    }

    private ClaimsPrincipal? TryValidateToken(string token)
    {
        try
        {
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = settings.Issuer,
                ValidateAudience = true,
                ValidAudience = settings.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret)),
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
