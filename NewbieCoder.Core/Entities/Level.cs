namespace NewbieCoder.Core.Entities;

/// <summary>
/// Represents an interview question difficulty level.
/// Examples: Entry, Junior, Middle, Senior, Expert.
/// </summary>
public sealed class Level : BaseEntity
{
    /// <summary>Unique code identifier, e.g. "ENTRY", "JUNIOR".</summary>
    public string Code { get; set; } = null!;

    /// <summary>Display name, e.g. "Entry".</summary>
    public string Name { get; set; } = null!;

    /// <summary>Optional description of the level.</summary>
    public string? Description { get; set; }

    /// <summary>Display order for sorting. Must be >= 0.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Whether the level is active and available for use.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Navigation to associated interview questions.</summary>
    public ICollection<InterviewQuestion> InterviewQuestions { get; set; } = new List<InterviewQuestion>();
}
