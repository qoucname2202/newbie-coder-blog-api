namespace NewbieCoder.Core.Entities;


/// <summary>
/// Base Entity
/// </summary>
public abstract class BaseEntity
{
    public long Id { get; set; }
    public DateTimeOffset EffDate { get; set; }
    public DateTimeOffset DateLastMaint { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public long? DeletedBy { get; set; }
}

