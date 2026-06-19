using System.Net;


namespace NewbieCoder.Core.Entities
{
    public class Tag : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = "ACT";
        public int PostCount { get; set; }
        public int QuestionCount { get; set; }
        public int FollowerCount { get; set; }
    }

}
