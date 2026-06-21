using System.Net;
namespace NewbieCoder.Core.Entities
{
    public class InterviewQuestion : BaseEntity
    {
        public long? AuthorId { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string QuestionContent { get; set; } = null!;
        public string Level { get; set; } = null!;
        public string? Topic { get; set; }
        public string Status { get; set; } = "DRAFT";
        public int ViewCount { get; set; }
        public int VoteScore { get; set; }
        public int BookmarkCount { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public User? Author { get; set; }
        public ICollection<InterviewAnswer> Answers { get; set; } = new List<InterviewAnswer>();
        public ICollection<InterviewQuestionTag> InterviewQuestionTags { get; set; } = new List<InterviewQuestionTag>();
    }
}
