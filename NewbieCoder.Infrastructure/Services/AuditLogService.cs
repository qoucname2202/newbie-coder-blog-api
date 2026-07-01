using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Services;

public sealed class AuditLogService(AppDbContext db) : IAuditLogService
{
    public async Task LogAsync(
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
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog
        {
            UserId = userId,
            Email = email,
            SessionId = sessionId,
            DeviceId = deviceId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.AuditLogs.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
    }
}
