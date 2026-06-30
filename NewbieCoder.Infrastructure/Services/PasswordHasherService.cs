using NewbieCoder.Core.Constants;
using NewbieCoder.Core.Interfaces.Services;

namespace NewbieCoder.Infrastructure.Services;

public sealed class PasswordHasherService : IPasswordHasherService
{
    public string Hash(string plainPassword)
        => BCrypt.Net.BCrypt.HashPassword(plainPassword, AuthConstants.BcryptWorkFactor);

    public bool Verify(string plainPassword, string hash)
        => BCrypt.Net.BCrypt.Verify(plainPassword, hash);
}
