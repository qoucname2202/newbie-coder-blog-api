using System.Net;
namespace NewbieCoder.Core.Entities
{
    public class RolePermission : BaseEntity
    {
        public long RoleId { get; set; }
        public long PermissionId { get; set; }
        public long? CreatedBy { get; set; }

        public Role Role { get; set; } = null!;
        public Permission Permission { get; set; } = null!;
        public User? CreatedByUser { get; set; }
    }
}
