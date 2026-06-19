using System.Net;

namespace NewbieCoder.Core.Entities
{
    public class PasswordResetToken : BaseEntity
    {
        public long UserId { get; set; }
        public string TokenHash { get; set; } = null!;
        public string Status { get; set; } = "ACT";
        public string? RequestedIp { get; set; }
        public string? RequestedUserAgent { get; set; }
        public DateTimeOffset ExpiredAt { get; set; }
        public DateTimeOffset? UsedAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
    }
}
