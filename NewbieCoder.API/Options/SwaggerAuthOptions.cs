namespace NewbieCoder.API.Options;

/// <summary>
/// Configuration options for Swagger Basic Authentication, loaded from environment variables.
/// </summary>
public sealed class SwaggerAuthOptions
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
