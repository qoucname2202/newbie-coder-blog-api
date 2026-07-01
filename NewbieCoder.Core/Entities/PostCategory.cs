using NewbieCoder.Core.Enums;
namespace NewbieCoder.Core.Entities
{

    public class PostCategory : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        public long? ParentId { get; set; }
        public EntityStatus Status { get; set; } = EntityStatus.Active;

        public PostCategory? Parent { get; set; }
        public ICollection<PostCategory> Children { get; set; } = new List<PostCategory>();
        public ICollection<Post> Posts { get; set; } = new List<Post>();

    }
}
