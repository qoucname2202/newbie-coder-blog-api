
using System.Net;
namespace NewbieCoder.Core.Entities
{
    public class InterviewAnswer : BaseEntity
    {
        public long QuestionId { get; set; }
        public string AnswerContent { get; set; } = null!;
        public string? Explanation { get; set; }
        public string? Example { get; set; }
        public bool IsOfficial { get; set; } = true;
    }
}
