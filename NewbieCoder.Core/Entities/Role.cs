using System.Net;

namespace NewbieCoder.Core.Entities
{
    public class Role : BaseEntity
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
        public string Status { get; set; } = "ACT";
    }
}
