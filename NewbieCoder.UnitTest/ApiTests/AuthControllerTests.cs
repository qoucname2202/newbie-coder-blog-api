using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NewbieCoder.API.Controllers;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Auth;
using NewbieCoder.Core.DTOs.Response.Auth;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Services;
using Xunit;

namespace NewbieCoder.UnitTest.ApiTests;

/// <summary>
/// Integration tests for AuthController using WebApplicationFactory.
/// Uses test doubles for IAuthService to cover all HTTP response paths.
/// </summary>
public class AuthControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuthControllerTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    private void ResetRateLimit() => _factory.RateLimitService.Reset();
    private void ResetLoginState() => _factory.ClearLoginState();

    /// <summary>
    /// Calls POST /api/v1/auth/login with the current TestAuthService setup, extracts the
    /// access token from the response and returns an HttpClient that sends it as
    /// Authorization: Bearer … on every request. Use this helper to test protected endpoints
    /// like GET /me or POST /logout without manually constructing JWT headers.
    /// </summary>
    private async Task<HttpClient> GetAuthenticatedClientAsync(
        CancellationToken cancellationToken = default)
    {
        ResetRateLimit();
        ResetLoginState();

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest { LoginId = "test@example.com", Password = "Password@123" },
            _jsonOptions,
            cancellationToken);

        loginResponse.EnsureSuccessStatusCode();
        var body = await loginResponse.Content.ReadFromJsonAsync<AuthTokenResponse>(_jsonOptions, cancellationToken);
        Assert.NotNull(body);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return client;
    }

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndTokens()
    {
        ResetRateLimit();
        ResetLoginState();
        // Arrange
        _factory.SetLoginResult(new AuthTokenResponse
        {
            AccessToken = "access-token-fake",
            RefreshTokenJwt = "refresh-token-jwt-fake",
            TokenType = "Bearer",
            ExpiresIn = 900
        });

        var request = new LoginRequest { LoginId = "test@example.com", Password = "Password@123", RememberMe = true };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal("access-token-fake", body.AccessToken);
        Assert.Equal("refresh-token-jwt-fake", body.RefreshTokenJwt);
        Assert.Equal("Bearer", body.TokenType);
        Assert.Equal(900, body.ExpiresIn);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        ResetRateLimit();
        ResetLoginState();
        // Arrange
        _factory.SetLoginException(new BusinessException(
            ResponseMessages.InvalidCredentials,
            statusCode: HttpStatusCodes.Unauthorized,
            responseCode: ResponseCodes.Unauthorized));

        var request = new LoginRequest { LoginId = "wrong@example.com", Password = "BadPassword!" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthErrorResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal(ResponseCodes.Unauthorized, body.ResponseStatus.ResponseCode);
    }

    [Fact]
    public async Task Login_WithBlockedUser_Returns403()
    {
        ResetRateLimit();
        ResetLoginState();
        // Arrange
        _factory.SetLoginException(new BusinessException(
            ResponseMessages.UserBlocked,
            statusCode: HttpStatusCodes.Forbidden,
            responseCode: ResponseCodes.Forbidden));

        var request = new LoginRequest { LoginId = "blocked@example.com", Password = "Password@123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthErrorResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal(ResponseCodes.Forbidden, body.ResponseStatus.ResponseCode);
        Assert.Equal(ResponseMessages.UserBlocked, body.ResponseStatus.ResponseMessage);
    }

    [Fact]
    public async Task Login_WithBlockedDevice_Returns403()
    {
        ResetRateLimit();
        ResetLoginState();
        // Arrange
        _factory.SetLoginException(new BusinessException(
            ResponseMessages.DeviceBlocked,
            statusCode: HttpStatusCodes.Forbidden,
            responseCode: ResponseCodes.Forbidden));

        var request = new LoginRequest { LoginId = "user@example.com", Password = "Password@123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthErrorResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal(ResponseCodes.Forbidden, body.ResponseStatus.ResponseCode);
    }

    [Fact]
    public async Task Login_WithMissingLoginId_Returns400()
    {
        ResetRateLimit();
        ResetLoginState();
        // Arrange — model validation handles this at the action level.
        var request = new { Password = "Password@123" }; // login_id intentionally omitted

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithShortPassword_Returns400()
    {
        ResetRateLimit();
        ResetLoginState();
        // Arrange — password too short.
        var request = new { LoginId = "test@example.com", Password = "1234567" }; // 7 chars

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithRateLimitExceeded_Returns429()
    {
        ResetRateLimit();
        ResetLoginState();
        // Arrange
        _factory.SetRateLimitBlocked(true);

        var request = new LoginRequest { LoginId = "test@example.com", Password = "Password@123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthErrorResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal(ResponseCodes.TooManyRequests, body.ResponseStatus.ResponseCode);
    }

    #endregion

    #region Authenticated Endpoints Tests

    [Fact]
    public async Task Logout_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LogoutAll_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/auth/logout-all", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Happy-path for GET /me: login to obtain a bearer token, call /me, assert 200 + user data.
    /// </summary>
    [Fact]
    public async Task GetMe_WithValidToken_Returns200()
    {
        using var client = await GetAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserInfoResponse>>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal("000000", body!.ResponseStatus.ResponseCode);
        Assert.Equal("test@example.com", body.ResponseData!.Email);
    }

    /// <summary>
    /// After logout, the same token should no longer be accepted — /me returns 401.
    /// </summary>
    [Fact]
    public async Task GetMe_AfterLogout_Returns401()
    {
        using var client = await GetAuthenticatedClientAsync();

        // Confirm token is valid before logout.
        var beforeLogout = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, beforeLogout.StatusCode);

        // Logout — this invalidates the current session.
        var logoutResponse = await client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // Re-use the same (now-revoked) token — must be rejected.
        var afterLogout = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    #endregion
}

// =============================================================================
// Test doubles
// =============================================================================

/// <summary>
/// Custom WebApplicationFactory that replaces real services with in-memory test doubles.
/// </summary>
public sealed class AuthWebApplicationFactory : TestWebApplicationFactory
{
    private readonly TestAuthService _authService = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            // Remove real IAuthService (both singleton and factory registrations).
            var authDescriptors = services.Where(sd => sd.ServiceType == typeof(IAuthService)).ToList();
            foreach (var d in authDescriptors) services.Remove(d);
            services.AddSingleton<IAuthService>(_authService);
        });
    }

    // Helper methods to configure test doubles from test cases.
    internal void SetLoginResult(AuthTokenResponse response) => _authService.LoginResult = response;
    internal void SetLoginException(Exception ex) => _authService.LoginException = ex;
    internal void ClearLoginState() => _authService.Reset();
}

/// <summary>
/// In-memory test double for IAuthService.
/// </summary>
public sealed class TestAuthService : IAuthService
{
    public AuthTokenResponse? LoginResult { get; set; }
    public Exception? LoginException { get; set; }

    // Tracks the last access token generated by this service (for session/revocation logic).
    private string? _lastAccessToken;
    private long _currentSessionId = 1;
    private readonly HashSet<string> _revokedTokens = new();

    public void Reset()
    {
        LoginResult = null;
        LoginException = null;
        _lastAccessToken = null;
        _currentSessionId = 1;
        _revokedTokens.Clear();
    }

    public Task<AuthTokenResponse> LoginAsync(
        LoginRequest request, string? deviceId, string? deviceName,
        string? deviceType, string? userAgent, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (LoginException != null) throw LoginException;
        if (LoginResult != null) return Task.FromResult(LoginResult);

        var accessToken = GenerateAccessToken(
            userId: 1,
            email: "test@example.com",
            username: "testuser",
            roles: new List<string> { "USER" },
            sessionId: _currentSessionId,
            deviceId: 1);

        _lastAccessToken = accessToken;
        return Task.FromResult(new AuthTokenResponse
        {
            AccessToken = accessToken,
            RefreshTokenJwt = "test-refresh-jwt",
            TokenType = "Bearer",
            ExpiresIn = 900
        });
    }

    public Task LogoutAsync(
        long userId,
        long sessionId,
        string? refreshToken,
        LogoutReason logoutReason,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        // Mark the current access token as revoked so AuthMiddleware rejects it.
        if (_lastAccessToken != null)
            _revokedTokens.Add(_lastAccessToken);
        _currentSessionId++;
        return Task.CompletedTask;
    }

    public Task LogoutAllAsync(
        long userId,
        long? currentSessionId,
        bool keepCurrentSession,
        LogoutReason logoutReason,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<UserInfoResponse> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new UserInfoResponse
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            FullName = "Test User",
            Roles = new List<string> { "USER" }
        });

    private const string TestSecret = "TestSecretKeyThatIsAtLeast32CharactersLongForJwt!";
    private const string TestIssuer = "NewbieCoderAPI";
    private const string TestAudience = "NewbieCoderClient";

    public string GenerateAccessToken(
        long userId, string email, string username,
        IReadOnlyList<string> roles,
        long sessionId, long deviceId)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtClaimTypes.Username, username),
            new(JwtClaimTypes.SessionId, sessionId.ToString()),
            new(JwtClaimTypes.DeviceId, deviceId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(15).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public long? ValidateAndGetUserId(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = TestIssuer,
                ValidateAudience = true,
                ValidAudience = TestAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret)),
                ClockSkew = TimeSpan.Zero
            };
            var principal = handler.ValidateToken(token, parameters, out _);
            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return long.TryParse(sub, out var userId) ? userId : null;
        }
        catch
        {
            return null;
        }
    }

    public bool IsTokenRevoked(string token) => _revokedTokens.Contains(token);

    public string GenerateRefreshToken(
        long userId, string email,
        IReadOnlyList<string> roles,
        long sessionId, long deviceId) =>
        "test-refresh-jwt";
}

/// <summary>
/// In-memory test double for IAuthRateLimitService.
/// </summary>
public sealed class TestRateLimitService : IAuthRateLimitService
{
    private bool _blocked;

    public void SetBlocked(bool blocked) => _blocked = blocked;
    public void Reset() => _blocked = false;

    public bool IsBlocked(string loginId, string ipAddress) => _blocked;
    public void RecordFailedAttempt(string loginId, string ipAddress) { }
    public void ClearAttempts(string loginId, string ipAddress) { }
}

/// <summary>
/// Minimal API response envelope used in tests (matches NewbieCoder.Core.ViewModels.ApiResponse<T>).
/// </summary>
public sealed class TestApiResponse<T>
{
    public required string RequestTrace { get; init; }
    public required string ResponseDateTime { get; init; }
    public T? ResponseData { get; init; }
    public required TestResponseStatus ResponseStatus { get; init; }
}

public sealed class TestResponseStatus
{
    public required string ResponseCode { get; init; }
    public required string ResponseMessage { get; init; }
    public string? TracingMessage { get; init; }
}
