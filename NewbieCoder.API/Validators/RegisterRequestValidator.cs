using System.Text.RegularExpressions;
using FluentValidation;
using NewbieCoder.Core.DTOs.Request.Auth;
using NewbieCoder.Core.Validation;

namespace NewbieCoder.API.Validators;

/// <summary>
/// Validates RegisterRequest — enforces Email, Username, Password, FullName, and AcceptTerms rules.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    // Detects any HTML / XML angle-bracket payload (e.g. "<b>admin</b>", "<script>alert(1)</script>").
    private static readonly Regex HtmlTagPattern =
        new(@"<[^>]+>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Reserved system account names that users are not allowed to register with.
    /// Covers: admin, support, moderator, system, official, bot, developer role names,
    /// and common variations with separators or number suffixes.
    /// </summary>
    private static readonly string[] ReservedSystemAccounts =
    [
        // Exact reserved names
        "admin", "administrator", "moderator", "mod", "support", "staff",
        "system", "root", "superuser", "owner", "founder", "ceo", "cto",
        "official", "bot", "chatbot", "api", "dev", "developer",
        "helpdesk", "noreply", "no-reply", "security", "verified",
        "customer-service", "customer_service",

        // Admin family
        "admin1", "admin123", "admin_official", "admin_officiel",
        "admin_dev", "admins", "administration",

        // Support / help family
        "support1", "support_official", "support_team", "support_dev",
        "supportadmin", "helps", "helpdesk",

        // Dev / developer family
        "dev1", "dev_official", "dev_account", "devs", "developer1",
        "devadmin", "devteam",

        // Moderator family
        "mod1", "mod_official", "moderators", "moderator1", "modaccount",

        // System / official family
        "system1", "system_official", "official1", "officialadmin",
        "official_dev", "official_support", "official_account",
        "verified1", "verified_official",

        // Bot family
        "bot1", "bot_official", "chatbot1", "chats",

        // Staff / team family
        "staff1", "staff_official", "staff_account",
        "team", "team1", "team_official", "teamaccount",
        "devteam", "adminteam",

        // Service / api family
        "api1", "service", "service_account",
    ];

    /// <summary>
    /// Regex patterns to strip common separators / suffixes from a username
    /// before comparing against the reserved list.
    /// </summary>
    private static readonly Regex[] SeparatorStripPatterns =
    [
        // Strip separators: "dev_huong" → "devhuong", "admin-official" → "adminofficial"
        new(@"[-_]", RegexOptions.Compiled),
    ];

    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        // ── Username rules ──────────────────────────────────────────────────
        RuleFor(x => x.Username)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Username is required.")
            // Rule 0: Reject HTML / XML tags.
            .Must(username => username == null || !HtmlTagPattern.IsMatch(username))
            .WithMessage("Username must not contain HTML tags.")
            // Rule 1: Reject whitespace characters.
            .Must(username => username == null || !username.Any(char.IsWhiteSpace))
            .WithMessage("Username must not contain whitespace.")
            // Rule 2: Length bounds.
            .MinimumLength(UsernameAttribute.MinLength)
            .WithMessage($"Username must be at least {UsernameAttribute.MinLength} characters.")
            .MaximumLength(UsernameAttribute.MaxLength)
            .WithMessage($"Username must not exceed {UsernameAttribute.MaxLength} characters.")
            // Rule 3: Allowed character set.
            .Matches(@"^[a-z0-9_-]+$")
            .WithMessage("Username may only contain lowercase letters, numbers, underscores, and hyphens.")
            // Rule 4: Must contain at least one letter.
            .Must(username => username != null && username.Any(char.IsLetter))
            .WithMessage("Username must contain at least one letter.")
            // Rule 5: Cannot be all digits.
            .Must(username => username != null && !username.All(char.IsDigit))
            .WithMessage("Username cannot contain only numbers.")
            // Rule 6: Cannot start or end with underscore / hyphen.
            .Must(username => username != null && username.Length > 1 &&
                              !char.IsPunctuation(username[^1]) &&
                              !char.IsPunctuation(username[0]))
            .WithMessage("Username must not start or end with an underscore or hyphen.")
            // Rule 7: No consecutive underscores or hyphens.
            .Must(username => username != null &&
                              !username.Contains("__") &&
                              !username.Contains("--") &&
                              !username.Contains("_-") &&
                              !username.Contains("-_"))
            .WithMessage("Username must not contain consecutive underscores or hyphens.")
            // Rule 8: Reject system account impersonation.
            .Must(username => username == null || !IsSystemAccountImpersonation(username))
            .WithMessage("This username is reserved and cannot be used.");

        // ── Password rules ──────────────────────────────────────────────────
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordStrengthAttribute.MinLength)
            .WithMessage($"Password must be at least {PasswordStrengthAttribute.MinLength} characters.")
            .MaximumLength(PasswordStrengthAttribute.MaxLength)
            .WithMessage($"Password must not exceed {PasswordStrengthAttribute.MaxLength} characters.")
            .Must(password => password != null && !CommonPasswords.IsBlocked(password))
            .WithMessage("This password is too common. Please choose a stronger password.");

        // ── Confirm password rules ──────────────────────────────────────────
        // ConfirmPassword must be validated independently — it must NOT be gated on
        // Password being valid, otherwise a bad password hides confirm-password errors.
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required.")
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match.");

        // ── Full name & terms ──────────────────────────────────────────────
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            // Reject HTML/JS injection payloads (e.g. "<script>alert(1)</script>").
            .Must(fullName => fullName == null || !HtmlTagPattern.IsMatch(fullName))
            .WithMessage("Full name must not contain HTML tags.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(150).WithMessage("Full name must not exceed 150 characters.")
            // Validate against the trimmed value — this mirrors what SanitizeFullName() does.
            // "  A  " → trim → "A" → length=1 < 2 → FAIL.
            .Must(fullName => fullName == null || fullName.Trim().Length >= 2)
            .WithMessage("Full name must be at least 2 characters.")
            // "  AB..." (200 chars) → trim → 198 > 150 → FAIL.
            .Must(fullName => fullName == null || fullName.Trim().Length <= 150)
            .WithMessage("Full name must not exceed 150 characters.")
            // Only letters (A-Z, a-z, Vietnamese diacritics) and spaces — no digits.
            .Matches(@"^[\p{L} ]+$")
            .WithMessage("Full name must contain only letters and spaces.")
            .Must(fullName => fullName == null || fullName.Trim().Length == fullName.Length)
            .WithMessage("Full name must not contain leading or trailing whitespace.");

        RuleFor(x => x.AcceptTerms)
            .Must(acceptTerms => acceptTerms == true)
            .WithMessage("You must accept the terms of service.");
    }

    /// <summary>
    /// Checks whether the given username matches or resembles a reserved system account.
    /// Comparison is done against the raw username AND after stripping common separators/number suffixes.
    /// </summary>
    private static bool IsSystemAccountImpersonation(string username)
    {
        ArgumentNullException.ThrowIfNull(username);
        var lower = username.ToLowerInvariant();

        // Direct match.
        if (ReservedSystemAccounts.Contains(lower))
            return true;

        // Strip separators and number suffixes, then re-check.
        var stripped = lower;
        foreach (var pattern in SeparatorStripPatterns)
            stripped = pattern.Replace(stripped, "");

        return ReservedSystemAccounts.Contains(stripped);
    }
}