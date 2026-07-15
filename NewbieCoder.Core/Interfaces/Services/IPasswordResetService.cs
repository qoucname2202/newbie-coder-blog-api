using NewbieCoder.Core.DTOs.Response.Auth;

namespace NewbieCoder.Core.Interfaces.Services;

public interface IPasswordResetService
{
    /// <summary>Initiates a password-reset flow for the given email address.</summary>
    Task InitiateResetAsync(
        string email,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>Completes the password-reset flow using the plain token from the email.</summary>
    Task<ResetPasswordSuccessResponse> CompleteResetAsync(
        string plainToken,
        string newPassword,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}
