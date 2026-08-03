using Microsoft.Extensions.Options;
using NewbieCoder.API.Options;

namespace NewbieCoder.API.Options;

/// <summary>
/// Configures <see cref="SwaggerAuthOptions"/> by reading credentials from environment variables,
/// falling back to app settings. Throws at startup if required variables are missing.
/// </summary>
public sealed class SwaggerAuthOptionsValidator : IConfigureOptions<SwaggerAuthOptions>
{
    public void Configure(SwaggerAuthOptions options)
    {
        options.Username = Resolve("SWAGGER_USERNAME", "SwaggerAuth:Username");
        options.Password = Resolve("SWAGGER_PASSWORD", "SwaggerAuth:Password");
    }

    private static string Resolve(string envKey, string configKey)
    {
        var value = Environment.GetEnvironmentVariable(envKey)
            ?? throw new InvalidOperationException(
                $"{envKey} is required for Swagger Basic Auth. Set it in the .env file.");
        return value;
    }
}
