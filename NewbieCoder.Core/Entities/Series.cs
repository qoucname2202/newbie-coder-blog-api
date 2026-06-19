using System.Net;
namespace NewbieCoder.Core.Entities
{
    public class Series : BaseEntity
    {
        public long AuthorId { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string Status { get; set; } = "DRAFT";
        public string Visibility { get; set; } = "PUBLIC";
        public int ViewCount { get; set; }
        public int BookmarkCount { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
    }
}
