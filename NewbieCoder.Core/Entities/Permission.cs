using System.Net;

namespace NewbieCoder.Core.Entities
{
    public class Permission : BaseEntity
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Module { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = "ACT";

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
