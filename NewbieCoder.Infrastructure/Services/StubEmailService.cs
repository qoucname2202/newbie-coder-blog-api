using Microsoft.Extensions.Logging;
using NewbieCoder.Core.Interfaces.Services;

namespace NewbieCoder.Infrastructure.Services;

/// <summary>
/// Stub email service that logs the reset link instead of sending a real email.
/// Replace with a real provider (SMTP, SendGrid, Mailgun, etc.) before going to production.
/// </summary>
public sealed class StubEmailService : IEmailService
{
    private readonly ILogger<StubEmailService> _logger;

    public StubEmailService(ILogger<StubEmailService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendPasswordResetEmailAsync(
        string toEmail,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[STUB EMAIL] To: {Email} | Reset link: {ResetLink}",
            toEmail,
            resetLink);

        // In production, replace with actual email dispatch logic.
        // For now we always return true so the flow continues even without a real provider.
        return Task.FromResult(true);
    }
}
