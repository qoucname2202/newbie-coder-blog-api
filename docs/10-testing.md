# 10 — Testing

Testing strategy for Newbie Coder API. Response envelope assertions must use the **standard format** (see [05-api-contracts.md](05-api-contracts.md)).

---

## ApiResponse unit tests

**File:** `CoreTests/ApiResponseTests.cs`

```csharp
[Fact]
public void Success_SetsEnvelopeFields()
{
    var response = ApiResponse<string>.Success("OK", requestTrace, responseDateTime: fixedTime);

    Assert.Equal(ResponseCodes.Success, response.ResponseStatus.ResponseCode);
    Assert.Equal(ResponseMessages.Success, response.ResponseStatus.ResponseMessage);
    Assert.Equal("OK", response.ResponseData);
    Assert.Null(response.ResponseStatus.TracingMessage);
}

[Fact]
public void Fail_SetsEmptyResponseDataAndErrorStatus()
{
    var response = ApiResponse<string>.Fail(
        requestTrace, ResponseCodes.ValidationError, "Error Message", responseDateTime: fixedTime);

    Assert.Equal(string.Empty, response.ResponseData);
    Assert.Equal(ResponseCodes.ValidationError, response.ResponseStatus.ResponseCode);
}
```

Use `ResponseCodes` constants in assertions — not `"000000"` literals.

---

## API integration tests

**File:** `ApiTests/HealthControllerTests.cs`

Minimum: HTTP 200 on `GET /api/health`.

Recommended enhanced assertions:

```csharp
var json = await response.Content.ReadAsStringAsync();
using var doc = JsonDocument.Parse(json);
Assert.Equal("000000", doc.RootElement.GetProperty("responseStatus").GetProperty("responseCode").GetString());
Assert.Equal("OK", doc.RootElement.GetProperty("responseData").GetString());
```

Verify `requestTrace` header: `response.Headers.GetValues("X-Request-Trace")`.

---

## Test pyramid

```text
ApiTests (integration) → ServiceTests → RepositoryTests → CoreTests (unit)
```

---

## Running tests

```powershell
dotnet test
dotnet test --configuration Release
```

CI: `.github/workflows/ci.yml` — must pass on Ubuntu.

---

## Checklist for new endpoints

- [ ] Success returns `responseCode: 000000`
- [ ] Failure returns `responseData: ""`
- [ ] Business errors use expected `ResponseCodes`
- [ ] `dotnet test` passes

See [07-security-and-clean-code.md](07-security-and-clean-code.md) for security-related test expectations (no secrets in responses).
