using NewbieCoder.Core.Enums;
namespace NewbieCoder.Core.Entities
{
    public class Role : BaseEntity
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
        public RoleStatus Status { get; set; } = RoleStatus.Active;


        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
