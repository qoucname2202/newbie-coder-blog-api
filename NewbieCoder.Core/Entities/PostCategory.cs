using System.Net;
namespace NewbieCoder.Core.Entities
{

    public class PostCategory : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        public long? ParentId { get; set; }
        public string Status { get; set; } = "ACT";
    }
}
