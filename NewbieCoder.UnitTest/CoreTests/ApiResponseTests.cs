using NewbieCoder.Core.Constants;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.UnitTest.CoreTests;

public class ApiResponseTests
{
    private const string RequestTrace = "25c89a97-717f-449f-a544-3ec8704767cd";
    private static readonly DateTimeOffset FixedTime =
        DateTimeOffset.Parse("2026-06-12T22:22:05+07:00");

    [Fact]
    public void Success_SetsEnvelopeFields()
    {
        var response = ApiResponse<string>.Success("OK", RequestTrace, responseDateTime: FixedTime);

        Assert.Equal(RequestTrace, response.RequestTrace);
        Assert.Equal("2026-06-12T22:22:05+07:00", response.ResponseDateTime);
        Assert.Equal("OK", response.ResponseData);
        Assert.Equal(ResponseCodes.Success, response.ResponseStatus.ResponseCode);
        Assert.Equal(ResponseMessages.Success, response.ResponseStatus.ResponseMessage);
        Assert.Null(response.ResponseStatus.TracingMessage);
    }

    [Fact]
    public void Fail_SetsEmptyResponseDataAndErrorStatus()
    {
        var response = ApiResponse<string>.Fail(
            RequestTrace,
            ResponseCodes.ValidationError,
            "Error Message",
            responseDateTime: FixedTime);

        Assert.Equal(RequestTrace, response.RequestTrace);
        Assert.Equal("2026-06-12T22:22:05+07:00", response.ResponseDateTime);
        Assert.Equal(string.Empty, response.ResponseData);
        Assert.Equal(ResponseCodes.ValidationError, response.ResponseStatus.ResponseCode);
        Assert.Equal("Error Message", response.ResponseStatus.ResponseMessage);
        Assert.Null(response.ResponseStatus.TracingMessage);
    }
}
