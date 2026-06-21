using System.Net;

namespace NewbieCoder.Core.Entities
{
    public class CommunityAnswer : BaseEntity
    {
        public long QuestionId { get; set; }
        public long AuthorId { get; set; }
        public string Content { get; set; } = null!;
        public int VoteScore { get; set; }
        public bool IsAccepted { get; set; }

        public CommunityQuestion Question { get; set; } = null!;
        public User Author { get; set; } = null!;
    }
}
