namespace NewbieCoder.Core.ViewModels;

public class ResponseStatus
{
    public required string ResponseCode { get; init; }
    public required string ResponseMessage { get; init; }
    public string? TracingMessage { get; init; }
}
