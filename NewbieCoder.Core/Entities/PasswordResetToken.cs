using NewbieCoder.Core.Enums;
namespace NewbieCoder.Core.Entities
{
    public class PasswordResetToken : BaseEntity
    {
        public long UserId { get; set; }
        public string TokenHash { get; set; } = null!;
        public TokenStatus Status { get; set; } = TokenStatus.Active;
        public string? RequestedIp { get; set; }
        public string? RequestedUserAgent { get; set; }
        public DateTimeOffset ExpiredAt { get; set; }
        public DateTimeOffset? UsedAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
