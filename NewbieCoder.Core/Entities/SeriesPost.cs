using System.Net;

namespace NewbieCoder.Core.Entities
{
    public class SeriesPost
    {
        public long SeriesId { get; set; }
        public long PostId { get; set; }
        public int Position { get; set; } = 1;
        public DateTimeOffset EffDate { get; set; }


        public Series Series { get; set; } = null!;
        public Post Post { get; set; } = null!;
    }
}
