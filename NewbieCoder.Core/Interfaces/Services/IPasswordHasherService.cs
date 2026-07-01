namespace NewbieCoder.Core.Interfaces.Services;

/// <summary>
/// Verifies and hashes passwords using BCrypt.
/// </summary>
public interface IPasswordHasherService
{
    /// <summary>
    /// Hashes a plain-text password. Never call this for login verification — use Verify instead.
    /// </summary>
    string Hash(string plainPassword);

    /// <summary>
    /// Verifies a plain-text password against a stored BCrypt hash.
    /// </summary>
    bool Verify(string plainPassword, string hash);
}
