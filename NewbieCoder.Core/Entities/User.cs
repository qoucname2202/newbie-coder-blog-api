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

        public string Location { get; set; } = null!;

        public string? LastKnownIp { get; set; }
        public int? ReputationScore { get; set; }
        public long FollowerCount { get; set; }
        public long FollowingCount { get; set; }
        public int PostCount { get; set; }
        public bool EmailVerified { get; set; }
        public DateTimeOffset? EmailVerifiedAt { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }


        // Navigation
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();
        public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Series> Series { get; set; } = new List<Series>();
        public ICollection<CommunityQuestion> CommunityQuestions { get; set; } = new List<CommunityQuestion>();
        public ICollection<CommunityAnswer> CommunityAnswers { get; set; } = new List<CommunityAnswer>();
    }
}
