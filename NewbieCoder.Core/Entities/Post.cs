using System.Net;

namespace NewbieCoder.Core.Entities
{
    public class Post : BaseEntity
    {
        public long AuthorId { get; set; }
        public long? CategoryId { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Summary { get; set; }
        public string Content { get; set; } = null!;
        public string? ThumbnailUrl { get; set; }
        public string Status { get; set; } = "DRAFT";
        public string Visibility { get; set; } = "PUBLIC";
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public int VoteScore { get; set; }
        public int BookmarkCount { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
    }
}
