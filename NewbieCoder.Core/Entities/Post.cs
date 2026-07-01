using NewbieCoder.Core.Enums;
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
        public PostStatus Status { get; set; } = PostStatus.Draft;
        public PostVisibility Visibility { get; set; } = PostVisibility.Public;
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public int VoteScore { get; set; }
        public int BookmarkCount { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }

        public User Author { get; set; } = null!;
        public PostCategory? Category { get; set; }
        public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
        public ICollection<SeriesPost> SeriesPosts { get; set; } = new List<SeriesPost>();
    }
}
