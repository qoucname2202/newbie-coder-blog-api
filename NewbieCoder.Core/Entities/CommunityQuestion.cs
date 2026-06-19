using System.Net;

namespace NewbieCoder.Core.Entities
{
    public class CommunityQuestion : BaseEntity
    {
        public long AuthorId { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string Status { get; set; } = "OPEN";
        public long? AcceptedAnswerId { get; set; }
        public int ViewCount { get; set; }
        public int AnswerCount { get; set; }
        public int VoteScore { get; set; }
        public int BookmarkCount { get; set; }
        public DateTimeOffset? ClosedAt { get; set; }
    }
}
