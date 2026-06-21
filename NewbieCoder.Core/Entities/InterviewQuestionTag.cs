using System.Net;
namespace NewbieCoder.Core.Entities
{
    public class InterviewQuestionTag
    {
        public long QuestionId { get; set; }
        public long TagId { get; set; }
        public DateTimeOffset EffDate { get; set; }

        public InterviewQuestion Question { get; set; } = null!;
        public Tag Tag { get; set; } = null!;
    }
}
