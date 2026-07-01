using NewbieCoder.Core.Enums;
namespace NewbieCoder.Core.Entities
{
    public class LoginHistory : BaseEntity
    {
        public long? UserId { get; set; }
        public string? Email { get; set; }
        public long? DeviceId { get; set; }
        public long? SessionId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public LoginStatus LoginStatus { get; set; }
        public string? FailureReason { get; set; }

        public User? User { get; set; }
        public UserDevice? Device { get; set; }
        public UserSession? Session { get; set; }

    }
}
