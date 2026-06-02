namespace NewbieCoder.Core.Entities;

public class TodoItem : BaseEntity
{
    public required string Title { get; set; }
    public bool IsCompleted { get; set; }
}
