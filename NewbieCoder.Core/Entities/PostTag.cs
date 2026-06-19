using System.Net;

namespace NewbieCoder.Core.Entities
{
    public class PostTag
    {
        public long PostId { get; set; }
        public long TagId { get; set; }
        public DateTimeOffset EffDate { get; set; }
    }

}
