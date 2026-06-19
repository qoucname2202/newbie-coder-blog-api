using System.Net;
namespace NewbieCoder.Core.Entities
{
    public class UserSession : BaseEntity
    {
        public long UserId { get; set; }
        public long? DeviceId { get; set; }
        public string SessionTokenHash { get; set; } = null!;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string Status { get; set; } = "ACT";
        public DateTimeOffset LoginAt { get; set; }
        public DateTimeOffset? LastActiveAt { get; set; }
        public DateTimeOffset ExpiredAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
        public string? RevokedReason { get; set; }
    }
}
