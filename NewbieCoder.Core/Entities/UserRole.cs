using NewbieCoder.Core.Enums;
namespace NewbieCoder.Core.Entities
{
    public class UserRole : BaseEntity
    {
        public long UserId { get; set; }
        public long RoleId { get; set; }
        public long? AssignedBy { get; set; }
        public DateTimeOffset AssignedAt { get; set; }
        public DateTimeOffset? ExpiredAt { get; set; }
        public UserRoleStatus Status { get; set; } = UserRoleStatus.Active;

        public User User { get; set; } = null!;
        public Role Role { get; set; } = null!;
        public User? AssignedByUser { get; set; }
    }
}
