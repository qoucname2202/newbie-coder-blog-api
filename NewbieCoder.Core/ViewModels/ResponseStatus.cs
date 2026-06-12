
namespace NewbieCoder.Core.ViewModels
{
    public class ResponseStatus
    {
        public string ResponseCode { get; init; } = default!;
        public string ResponseMessage { get; init; } = default!;
        public string? TracingMessage { get; init; }
    }
}