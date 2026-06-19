using System.Net;

namespace NewbieCoder.Core.Entities
{
    public class UserRole : BaseEntity
    {
        public long UserId { get; set; }
        public long RoleId { get; set; }
        public long? AssignedBy { get; set; }
        public DateTimeOffset AssignedAt { get; set; }
        public DateTimeOffset? ExpiredAt { get; set; }
        public string Status { get; set; } = "ACT";
    }
}
