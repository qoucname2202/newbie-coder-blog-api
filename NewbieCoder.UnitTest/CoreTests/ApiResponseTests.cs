using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.UnitTest.CoreTests;

public class ApiResponseTests
{
    [Fact]
    public void Ok_SetsSuccessAndData()
    {
        var response = ApiResponse<string>.Ok("data", "ok");

        Assert.True(response.Success);
        Assert.Equal("data", response.Data);
        Assert.Equal("ok", response.Message);
    }

    [Fact]
    public void Fail_SetsSuccessFalse()
    {
        var response = ApiResponse<string>.Fail("error");

        Assert.False(response.Success);
        Assert.Equal("error", response.Message);
    }
}
