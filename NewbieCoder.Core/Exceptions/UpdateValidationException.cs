using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Response.User;

namespace NewbieCoder.Core.Exceptions;

/// <summary>
/// Exception carrying per-field validation errors for the update-profile endpoint.
/// The controller catches this and returns a structured errors array in the response.
/// </summary>
public sealed class UpdateValidationException : Exception
{
    public IReadOnlyList<FieldValidationError> Errors { get; }

    public UpdateValidationException(IReadOnlyList<(string field, string code, string message)> errors)
        : base(ResponseMessages.UpdateValidationFailed)
    {
        Errors = errors.Select(e => new FieldValidationError
        {
            Field = e.field,
            Code = e.code,
            Message = e.message
        }).ToList();
    }
}
