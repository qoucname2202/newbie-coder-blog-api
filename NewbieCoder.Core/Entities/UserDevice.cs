using NewbieCoder.Core.Enums;
namespace NewbieCoder.Core.Entities
{
    public class UserDevice : BaseEntity
    {
        public long UserId { get; set; }
        public string DeviceId { get; set; } = null!;
        public string? DeviceName { get; set; }
        public DeviceType DeviceType { get; set; } = DeviceType.Web;
        public string? Os { get; set; }
        public string? Browser { get; set; }
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }
        public DeviceStatus Status { get; set; } = DeviceStatus.Active;

        public User User { get; set; } = null!;
        public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
