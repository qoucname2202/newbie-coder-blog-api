using System.Text.RegularExpressions;

namespace NewbieCoder.Infrastructure.Services.Helpers;

/// <summary>
/// Generates URL-safe slugs from text strings.
/// </summary>
public static class SlugGenerator
{
    public static string Generate(string text)
    {
        var slug = text.ToLowerInvariant()
            .Trim()
            .Replace(" ", "-");

        slug = Regex.Replace(slug, @"[^a-z0-9-]", "");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        if (slug.Length > 200)
            slug = slug[..200].Trim('-');

        return string.IsNullOrWhiteSpace(slug) ? "question" : slug;
    }
}
