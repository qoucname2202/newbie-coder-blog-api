using System.Net;
namespace NewbieCoder.Core.Entities
{
    public class RolePermission : BaseEntity
    {
        public long RoleId { get; set; }
        public long PermissionId { get; set; }
        public long? CreatedBy { get; set; }
    }
}
