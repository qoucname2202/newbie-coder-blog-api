namespace NewbieCoder.Infrastructure.Data.SeedData;

using NewbieCoder.Core.Enums;

/// <summary>
/// Static seed data for test / development administrator accounts.
/// BCrypt work factor: 11. Hashes were pre-generated using the same algorithm
/// as <c>PasswordHasherService</c>.
/// </summary>
public static class UserSeedData
{
    /// <summary>BCrypt hash of <c>Admin@123456</c> (work factor 11). </summary>
    private const string AdminHash = "$2a$11$ddY/uDQbWtPQCW6h.bld/e7eRK5Wfq9gco8tAF/8yrxXBk2jJxFaS";

    /// <summary>BCrypt hash of <c>Test@123456</c> (work factor 11). </summary>
    private const string TestHash = "$2a$11$u9r/mEQb57jHRfrzj3GFH.9o9pr3gJIfP0FXwgGGqX41I5j5WDcFi";

    public static readonly List<UserSeedEntry> Accounts =
    [
        new UserSeedEntry
        {
            Email    = "phanchantay.ltp21@gmail.com",
            Username = "phanchan",
            FullName = "Phan Chan Tay",
            Password = AdminHash,
            Bio      = "System Administrator — NewbieCoder Blog",
            Location = "Ho Chi Minh City, Vietnam",
            Status   = UserStatus.Active,
            EmailVerified = true,
            Roles    = ["ADMIN"],
        },
        new UserSeedEntry
        {
            Email    = "duongquocnam224400@gmail.com",
            Username = "duongquocnam",
            FullName = "Duong Quoc Nam",
            Password = AdminHash,
            Bio      = "System Administrator — NewbieCoder Blog",
            Location = "Ho Chi Minh City, Vietnam",
            Status   = UserStatus.Active,
            EmailVerified = true,
            Roles    = ["ADMIN"],
        },
        new UserSeedEntry
        {
            Email    = "test@newbiecoder.local",
            Username = "testuser",
            FullName = "Test User",
            Password = TestHash,
            Bio      = "Test account for development and QA.",
            Location = "Local",
            Status   = UserStatus.Active,
            EmailVerified = false,
            Roles    = ["USER"],
        },
    ];

    public sealed class UserSeedEntry
    {
        public required string Email           { get; init; }
        public required string Username        { get; init; }
        public required string FullName        { get; init; }
        public required string Password        { get; init; }
        public string? Bio                    { get; init; }
        public string  Location               { get; init; } = "Unknown";
        public UserStatus Status { get; init; } = UserStatus.Active;
        public bool    EmailVerified           { get; init; }
        public required string[] Roles         { get; init; } = [];
    }
}
