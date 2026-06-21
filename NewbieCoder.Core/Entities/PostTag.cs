using System.Net;

namespace NewbieCoder.Core.Entities
{
    public class PostTag
    {
        public long PostId { get; set; }
        public long TagId { get; set; }
        public DateTimeOffset EffDate { get; set; }
        public Post Post { get; set; } = null!;
        public Tag Tag { get; set; } = null!;

    }

}
