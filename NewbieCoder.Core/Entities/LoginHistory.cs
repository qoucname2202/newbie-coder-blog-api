using System.Net;
namespace NewbieCoder.Core.Entities
{
    public class LoginHistory
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string? Email { get; set; }
        public long? DeviceId { get; set; }
        public long? SessionId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string LoginStatus { get; set; } = null!;
        public string? FailureReason { get; set; }
        public DateTimeOffset EffDate { get; set; }
        public DateTimeOffset DateLastMaint { get; set; }
    }
}
