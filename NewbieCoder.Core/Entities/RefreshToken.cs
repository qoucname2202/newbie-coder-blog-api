using System.Net;

namespace NewbieCoder.Core.Entities
{
    public class RefreshToken : BaseEntity
    {
        public long UserId { get; set; }
        public long SessionId { get; set; }
        public string TokenHash { get; set; } = null!;
        public Guid? TokenFamily { get; set; }
        public string Status { get; set; } = "ACT";
        public DateTimeOffset IssuedAt { get; set; }
        public DateTimeOffset ExpiredAt { get; set; }
        public DateTimeOffset? UsedAt { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }
        public long? ReplacedByTokenId { get; set; }
    }
}
