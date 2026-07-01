using NewbieCoder.Core.Enums;
namespace NewbieCoder.Core.Entities
{
    public class CommunityQuestion : BaseEntity
    {
        public long AuthorId { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Content { get; set; } = null!;
        public CommunityQuestionStatus Status { get; set; } = CommunityQuestionStatus.Open;
        public long? AcceptedAnswerId { get; set; }
        public int ViewCount { get; set; }
        public int AnswerCount { get; set; }
        public int VoteScore { get; set; }
        public int BookmarkCount { get; set; }
        public DateTimeOffset? ClosedAt { get; set; }

        public User Author { get; set; } = null!;
        public CommunityAnswer? AcceptedAnswer { get; set; }
        public ICollection<CommunityAnswer> Answers { get; set; } = new List<CommunityAnswer>();
        public ICollection<CommunityQuestionTag> CommunityQuestionTags { get; set; } = new List<CommunityQuestionTag>();
    }
}
