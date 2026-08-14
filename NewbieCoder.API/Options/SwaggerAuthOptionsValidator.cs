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
        var value = Environment.GetEnvironmentVariable(envKey);

        // In Testing environment (integration tests) the .env file is not present and Swagger
        // Basic Auth is skipped entirely — return a harmless placeholder so the DI container
        // still resolves successfully.  Real environments still require the real credentials.
        if (string.IsNullOrEmpty(value) &&
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing")
        {
            return $"testing-placeholder-{envKey.ToLowerInvariant()}";
        }

        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException(
                $"{envKey} is required for Swagger Basic Auth. Set it in the .env file.");
        }

        return value;
    }
}