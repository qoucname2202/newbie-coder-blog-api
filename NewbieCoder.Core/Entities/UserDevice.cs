using System.Net;

namespace NewbieCoder.Core.Entities
{
    public class UserDevice : BaseEntity
    {
        public long UserId { get; set; }
        public string DeviceId { get; set; } = null!;
        public string? DeviceName { get; set; }
        public string DeviceType { get; set; } = null!;
        public string? Os { get; set; }
        public string? Browser { get; set; }
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }
        public string Status { get; set; } = "ACT";
    }
}
