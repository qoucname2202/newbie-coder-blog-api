namespace NewbieCoder.Core.Interfaces.Services;

/// <summary>
/// Sends transactional emails. Implementation can use SMTP, SendGrid, Mailgun, etc.
/// </summary>
public interface IEmailService
{
    /// <summary>Sends a password-reset email to the user.</summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="resetLink">Full URL the user should click to reset their password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the email was accepted by the provider; false otherwise.</returns>
    Task<bool> SendPasswordResetEmailAsync(
        string toEmail,
        string resetLink,
        CancellationToken cancellationToken = default);
}
