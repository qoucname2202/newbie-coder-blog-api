namespace NewbieCoder.Infrastructure.Converters;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NewbieCoder.Core.Enums;

/// <summary>
/// Public enum-to-string converters for EF Core ValueConversion.
/// All methods are public so EF Core 8 can extract expressions for compiled models.
/// Field names use a "Converter" suffix to avoid shadowing the enum type names.
/// Uses v => ToDbString(v) lambdas so the compiler infers Expression&lt;Func&lt;...&gt;&gt;.
/// </summary>
public static class EnumStringConverters
{
    // ── UserStatus  ('ACT','INACT','BAN','CLS') ─────────────────────────────────
    public static string ToDbString(NewbieCoder.Core.Enums.UserStatus v) => v switch
    {
        NewbieCoder.Core.Enums.UserStatus.Active   => "ACT",
        NewbieCoder.Core.Enums.UserStatus.Inactive => "INACT",
        NewbieCoder.Core.Enums.UserStatus.Banned  => "BAN",
        NewbieCoder.Core.Enums.UserStatus.Closed   => "CLS",
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null)
    };
    public static NewbieCoder.Core.Enums.UserStatus FromDbStringUserStatus(string s) => s switch
    {
        "ACT"   => NewbieCoder.Core.Enums.UserStatus.Active,
        "INACT" => NewbieCoder.Core.Enums.UserStatus.Inactive,
        "BAN"   => NewbieCoder.Core.Enums.UserStatus.Banned,
        "CLS"   => NewbieCoder.Core.Enums.UserStatus.Closed,
        _ => throw new ArgumentException("Invalid UserStatus: " + s, nameof(s))
    };
    public static readonly ValueConverter<NewbieCoder.Core.Enums.UserStatus, string> UserStatusConverter =
        new(v => ToDbString(v), s => FromDbStringUserStatus(s));

    // ── RoleStatus  ('active','inactive') ──────────────────────────────────────
    public static string ToDbString(NewbieCoder.Core.Enums.RoleStatus v) => v switch
    {
        NewbieCoder.Core.Enums.RoleStatus.Active   => "active",
        NewbieCoder.Core.Enums.RoleStatus.Inactive => "inactive",
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null)
    };
    public static NewbieCoder.Core.Enums.RoleStatus FromDbStringRoleStatus(string s) => s switch
    {
        "active"   => NewbieCoder.Core.Enums.RoleStatus.Active,
        "inactive" => NewbieCoder.Core.Enums.RoleStatus.Inactive,
        _ => throw new ArgumentException("Invalid RoleStatus: " + s, nameof(s))
    };
    public static readonly ValueConverter<NewbieCoder.Core.Enums.RoleStatus, string> RoleStatusConverter =
        new(v => ToDbString(v), s => FromDbStringRoleStatus(s));

    // ── UserRoleStatus  ('active','revoked','expired') ─────────────────────────
    public static string ToDbString(NewbieCoder.Core.Enums.UserRoleStatus v) => v switch
    {
        NewbieCoder.Core.Enums.UserRoleStatus.Active  => "active",
        NewbieCoder.Core.Enums.UserRoleStatus.Revoked => "revoked",
        NewbieCoder.Core.Enums.UserRoleStatus.Expired => "expired",
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null)
    };
    public static NewbieCoder.Core.Enums.UserRoleStatus FromDbStringUserRoleStatus(string s) => s switch
    {
        "active"  => NewbieCoder.Core.Enums.UserRoleStatus.Active,
        "revoked" => NewbieCoder.Core.Enums.UserRoleStatus.Revoked,
        "expired" => NewbieCoder.Core.Enums.UserRoleStatus.Expired,
        _ => throw new ArgumentException("Invalid UserRoleStatus: " + s, nameof(s))
    };
    public static readonly ValueConverter<NewbieCoder.Core.Enums.UserRoleStatus, string> UserRoleStatusConverter =
        new(v => ToDbString(v), s => FromDbStringUserRoleStatus(s));

    // ── DeviceStatus  ('active','revoked','blocked') ──────────────────────────────
    public static string ToDbString(NewbieCoder.Core.Enums.DeviceStatus v) => v switch
    {
        NewbieCoder.Core.Enums.DeviceStatus.Active   => "active",
        NewbieCoder.Core.Enums.DeviceStatus.Revoked => "revoked",
        NewbieCoder.Core.Enums.DeviceStatus.Blocked => "blocked",
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null)
    };
    public static NewbieCoder.Core.Enums.DeviceStatus FromDbStringDeviceStatus(string s) => s switch
    {
        "active"  => NewbieCoder.Core.Enums.DeviceStatus.Active,
        "revoked" => NewbieCoder.Core.Enums.DeviceStatus.Revoked,
        "blocked" => NewbieCoder.Core.Enums.DeviceStatus.Blocked,
        _ => throw new ArgumentException("Invalid DeviceStatus: " + s, nameof(s))
    };
    public static readonly ValueConverter<NewbieCoder.Core.Enums.DeviceStatus, string> DeviceStatusConverter =
        new(v => ToDbString(v), s => FromDbStringDeviceStatus(s));

    // ── SessionStatus  ('active','expired','revoked') ────────────────────────────
    public static string ToDbString(NewbieCoder.Core.Enums.SessionStatus v) => v switch
    {
        NewbieCoder.Core.Enums.SessionStatus.Active  => "active",
        NewbieCoder.Core.Enums.SessionStatus.Expired => "expired",
        NewbieCoder.Core.Enums.SessionStatus.Revoked => "revoked",
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null)
    };
    public static NewbieCoder.Core.Enums.SessionStatus FromDbStringSessionStatus(string s) => s switch
    {
        "active"  => NewbieCoder.Core.Enums.SessionStatus.Active,
        "expired" => NewbieCoder.Core.Enums.SessionStatus.Expired,
        "revoked" => NewbieCoder.Core.Enums.SessionStatus.Revoked,
        _ => throw new ArgumentException("Invalid SessionStatus: " + s, nameof(s))
    };
    public static readonly ValueConverter<NewbieCoder.Core.Enums.SessionStatus, string> SessionStatusConverter =
        new(v => ToDbString(v), s => FromDbStringSessionStatus(s));

    // ── TokenStatus  ('active','used','revoked','expired') ──────────────────────
    public static string ToDbString(NewbieCoder.Core.Enums.TokenStatus v) => v switch
    {
        NewbieCoder.Core.Enums.TokenStatus.Active  => "active",
        NewbieCoder.Core.Enums.TokenStatus.Used    => "used",
        NewbieCoder.Core.Enums.TokenStatus.Revoked => "revoked",
        NewbieCoder.Core.Enums.TokenStatus.Expired => "expired",
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null)
    };
    public static NewbieCoder.Core.Enums.TokenStatus FromDbStringTokenStatus(string s) => s switch
    {
        "active"  => NewbieCoder.Core.Enums.TokenStatus.Active,
        "used"    => NewbieCoder.Core.Enums.TokenStatus.Used,
        "revoked" => NewbieCoder.Core.Enums.TokenStatus.Revoked,
        "expired" => NewbieCoder.Core.Enums.TokenStatus.Expired,
        _ => throw new ArgumentException("Invalid TokenStatus: " + s, nameof(s))
    };
    public static readonly ValueConverter<NewbieCoder.Core.Enums.TokenStatus, string> TokenStatusConverter =
        new(v => ToDbString(v), s => FromDbStringTokenStatus(s));

    // ── LoginStatus  ('success','failed') ─────────────────────────────────────
    public static string ToDbString(NewbieCoder.Core.Enums.LoginStatus v) => v switch
    {
        NewbieCoder.Core.Enums.LoginStatus.Success => "success",
        NewbieCoder.Core.Enums.LoginStatus.Failed  => "failed",
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null)
    };
    public static NewbieCoder.Core.Enums.LoginStatus FromDbStringLoginStatus(string s) => s switch
    {
        "success" => NewbieCoder.Core.Enums.LoginStatus.Success,
        "failed"  => NewbieCoder.Core.Enums.LoginStatus.Failed,
        _ => throw new ArgumentException("Invalid LoginStatus: " + s, nameof(s))
    };
    public static readonly ValueConverter<NewbieCoder.Core.Enums.LoginStatus, string> LoginStatusConverter =
        new(v => ToDbString(v), s => FromDbStringLoginStatus(s));

    // ── EntityStatus  ('active','inactive') ────────────────────────────────────
    public static string ToDbString(NewbieCoder.Core.Enums.EntityStatus v) => v switch
    {
        NewbieCoder.Core.Enums.EntityStatus.Active   => "active",
        NewbieCoder.Core.Enums.EntityStatus.Inactive => "inactive",
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null)
    };
    public static NewbieCoder.Core.Enums.EntityStatus FromDbStringEntityStatus(string s) => s switch
    {
        "active"   => NewbieCoder.Core.Enums.EntityStatus.Active,
        "inactive" => NewbieCoder.Core.Enums.EntityStatus.Inactive,
        _ => throw new ArgumentException("Invalid EntityStatus: " + s, nameof(s))
    };
    public static readonly ValueConverter<NewbieCoder.Core.Enums.EntityStatus, string> EntityStatusConverter =
        new(v => ToDbString(v), s => FromDbStringEntityStatus(s));

    // ── PostStatus  ('draft','pending','published','rejected','hidden','deleted') ─
    public static string ToDbString(NewbieCoder.Core.Enums.PostStatus v) => v switch
    {
        NewbieCoder.Core.Enums.PostStatus.Draft     => "draft",
        NewbieCoder.Core.Enums.PostStatus.Pending   => "pending",
        NewbieCoder.Core.Enums.PostStatus.Published => "published",
        NewbieCoder.Core.Enums.PostStatus.Rejected  => "rejected",
        NewbieCoder.Core.Enums.PostStatus.Hidden    => "hidden",
        NewbieCoder.Core.Enums.PostStatus.Deleted   => "deleted",
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null)
    };
    public static NewbieCoder.Core.Enums.PostStatus FromDbStringPostStatus(string s) => s switch
    {
        "draft"     => NewbieCoder.Core.Enums.PostStatus.Draft,
        "pending"   => NewbieCoder.Core.Enums.PostStatus.Pending,
        "published" => NewbieCoder.Core.Enums.PostStatus.Published,
        "rejected"  => NewbieCoder.Core.Enums.PostStatus.Rejected,
        "hidden"    => NewbieCoder.Core.Enums.PostStatus.Hidden,
        "deleted"   => NewbieCoder.Core.Enums.PostStatus.Deleted,
        _ => throw new ArgumentException("Invalid PostStatus: " + s, nameof(s))
    };
    public static readonly ValueConverter<NewbieCoder.Core.Enums.PostStatus, string> PostStatusConverter =
        new(v => ToDbString(v), s => FromDbStringPostStatus(s));

    // ── PostVisibility  ('public','private','link_only') ──────────────────────────
    public static string ToDbString(NewbieCoder.Core.Enums.PostVisibility v) => v switch
    {
        NewbieCoder.Core.Enums.PostVisibility.Public   => "public",
        NewbieCoder.Core.Enums.PostVisibility.Private  => "private",
        NewbieCoder.Core.Enums.PostVisibility.LinkOnly => "link_only",
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null)
    };
    public static NewbieCoder.Core.Enums.PostVisibility FromDbStringPostVisibility(string s) => s switch
    {
        "public"    => NewbieCoder.Core.Enums.PostVisibility.Public,
        "private"   => NewbieCoder.Core.Enums.PostVisibility.Private,
        "link_only" => NewbieCoder.Core.Enums.PostVisibility.LinkOnly,
        _ => throw new ArgumentException("Invalid PostVisibility: " + s, nameof(s))
    };
    public static readonly ValueConverter<NewbieCoder.Core.Enums.PostVisibility, string> PostVisibilityConverter =
        new(v => ToDbString(v), s => FromDbStringPostVisibility(s));

    // ── InterviewLevel  ('entry','junior','middle','senior','expert') ───────────
    public static string ToDbString(NewbieCoder.Core.Enums.InterviewLevel v) => v switch
    {
        NewbieCoder.Core.Enums.InterviewLevel.Entry  => "entry",
        NewbieCoder.Core.Enums.InterviewLevel.Junior => "junior",
        NewbieCoder.Core.Enums.InterviewLevel.Middle => "middle",
        NewbieCoder.Core.Enums.InterviewLevel.Senior => "senior",
        NewbieCoder.Core.Enums.InterviewLevel.Expert => "expert",
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null)
    };
    public static NewbieCoder.Core.Enums.InterviewLevel FromDbStringInterviewLevel(string s) => s switch
    {
        "entry"   => NewbieCoder.Core.Enums.InterviewLevel.Entry,
        "junior"  => NewbieCoder.Core.Enums.InterviewLevel.Junior,
        "middle"  => NewbieCoder.Core.Enums.InterviewLevel.Middle,
        "senior"  => NewbieCoder.Core.Enums.InterviewLevel.Senior,
        "expert"  => NewbieCoder.Core.Enums.InterviewLevel.Expert,
        _ => throw new ArgumentException("Invalid InterviewLevel: " + s, nameof(s))
    };
    public static readonly ValueConverter<NewbieCoder.Core.Enums.InterviewLevel, string> InterviewLevelConverter =
        new(v => ToDbString(v), s => FromDbStringInterviewLevel(s));

    // ── CommunityQuestionStatus  ('open','answered','resolved','closed','hidden') ──
    public static string ToDbString(NewbieCoder.Core.Enums.CommunityQuestionStatus v) => v switch
    {
        NewbieCoder.Core.Enums.CommunityQuestionStatus.Open     => "open",
        NewbieCoder.Core.Enums.CommunityQuestionStatus.Answered => "answered",
        NewbieCoder.Core.Enums.CommunityQuestionStatus.Resolved => "resolved",
        NewbieCoder.Core.Enums.CommunityQuestionStatus.Closed   => "closed",
        NewbieCoder.Core.Enums.CommunityQuestionStatus.Hidden   => "hidden",
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null)
    };
    public static NewbieCoder.Core.Enums.CommunityQuestionStatus FromDbStringCommunityQuestionStatus(string s) => s switch
    {
        "open"      => NewbieCoder.Core.Enums.CommunityQuestionStatus.Open,
        "answered"  => NewbieCoder.Core.Enums.CommunityQuestionStatus.Answered,
        "resolved"  => NewbieCoder.Core.Enums.CommunityQuestionStatus.Resolved,
        "closed"    => NewbieCoder.Core.Enums.CommunityQuestionStatus.Closed,
        "hidden"    => NewbieCoder.Core.Enums.CommunityQuestionStatus.Hidden,
        _ => throw new ArgumentException("Invalid CommunityQuestionStatus: " + s, nameof(s))
    };
    public static readonly ValueConverter<NewbieCoder.Core.Enums.CommunityQuestionStatus, string> CommunityQuestionStatusConverter =
        new(v => ToDbString(v), s => FromDbStringCommunityQuestionStatus(s));

    // ── DeviceType  ('web','mobile','tablet','desktop') ─────────────────────────
    public static string ToDbString(NewbieCoder.Core.Enums.DeviceType v) => v switch
    {
        NewbieCoder.Core.Enums.DeviceType.Web     => "web",
        NewbieCoder.Core.Enums.DeviceType.Mobile  => "mobile",
        NewbieCoder.Core.Enums.DeviceType.Tablet  => "tablet",
        NewbieCoder.Core.Enums.DeviceType.Desktop => "desktop",
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null)
    };
    public static NewbieCoder.Core.Enums.DeviceType FromDbStringDeviceType(string s) => s switch
    {
        "web"     => NewbieCoder.Core.Enums.DeviceType.Web,
        "mobile"  => NewbieCoder.Core.Enums.DeviceType.Mobile,
        "tablet"  => NewbieCoder.Core.Enums.DeviceType.Tablet,
        "desktop" => NewbieCoder.Core.Enums.DeviceType.Desktop,
        _ => throw new ArgumentException("Invalid DeviceType: " + s, nameof(s))
    };
    public static readonly ValueConverter<NewbieCoder.Core.Enums.DeviceType, string> DeviceTypeConverter =
        new(v => ToDbString(v), s => FromDbStringDeviceType(s));
}
