using System.Net;
namespace NewbieCoder.Core.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string Status { get; set; } = "INACT";
        public string? CoverUrl { get; set; }
        public string? GithubUrl { get; set; }
        public string? LinkedinUrl { get; set; }
        public string? LastKnownIp { get; set; }
        public int? ReputationScore { get; set; }
        public long FollowerCount { get; set; }
        public long FollowingCount { get; set; }
        public int PostCount { get; set; }
        public bool EmailVerified { get; set; }
        public DateTimeOffset? EmailVerifiedAt { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }
    }
}
