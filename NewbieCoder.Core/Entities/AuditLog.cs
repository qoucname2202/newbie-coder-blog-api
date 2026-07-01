using NewbieCoder.Core.Enums;

namespace NewbieCoder.Core.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string? Email { get; set; }
    public long? SessionId { get; set; }
    public long? DeviceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User? User { get; set; }
    public UserSession? Session { get; set; }
    public UserDevice? Device { get; set; }
}
