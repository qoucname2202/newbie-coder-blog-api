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

        public PostCategory? Parent { get; set; }
        public ICollection<PostCategory> Children { get; set; } = new List<PostCategory>();
        public ICollection<Post> Posts { get; set; } = new List<Post>();

    }
}
