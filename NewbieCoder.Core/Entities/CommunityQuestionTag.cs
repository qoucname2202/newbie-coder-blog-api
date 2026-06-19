using System.Net;

namespace NewbieCoder.Core.Entities
{
    public class CommunityQuestionTag
    {
        public long QuestionId { get; set; }
        public long TagId { get; set; }
        public DateTimeOffset EffDate { get; set; }
    }
}
