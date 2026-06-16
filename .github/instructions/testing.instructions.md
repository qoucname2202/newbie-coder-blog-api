---
applyTo: "NewbieCoder.UnitTest/**"
---

# Testing — NewbieCoder.UnitTest

## ApiResponse tests

Test the standard envelope:

```csharp
var response = ApiResponse<string>.Success("OK", requestTrace, responseDateTime: fixedTime);
Assert.Equal(ResponseCodes.Success, response.ResponseStatus.ResponseCode);
Assert.Equal("OK", response.ResponseData);

var fail = ApiResponse<string>.Fail(requestTrace, ResponseCodes.ValidationError, "Error Message");
Assert.Equal(string.Empty, fail.ResponseData);
Assert.Equal(ResponseCodes.ValidationError, fail.ResponseStatus.ResponseCode);
```

## API integration tests

Assert HTTP 200 for health; optionally deserialize and verify:

- `requestTrace` present
- `responseStatus.responseCode` == `"000000"`
- `responseData` == `"OK"`

## Rules

- Use `ResponseCodes` constants in assertions — not string literals
- Run `dotnet test` before completing tasks
- See [docs/10-testing.md](../../docs/10-testing.md)
