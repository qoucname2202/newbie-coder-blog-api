using NewbieCoder.Core.Enums;
namespace NewbieCoder.Core.Entities
{
    public class Series : BaseEntity
    {
        public long AuthorId { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public PostStatus Status { get; set; } = PostStatus.Draft;
        public PostVisibility Visibility { get; set; } = PostVisibility.Public;
        public int ViewCount { get; set; }
        public int BookmarkCount { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }

        public User Author { get; set; } = null!;
        public ICollection<SeriesPost> SeriesPosts { get; set; } = new List<SeriesPost>();
    }
}
