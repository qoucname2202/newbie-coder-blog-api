namespace NewbieCoder.Core.Interfaces.Services;

public interface IAuditLogService
{
    Task LogAsync(
        string action,
        long? userId = null,
        string? email = null,
        long? sessionId = null,
        long? deviceId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? entityType = null,
        long? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default);
}
