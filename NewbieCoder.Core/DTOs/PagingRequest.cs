using NewbieCoder.Core.Constants;

namespace NewbieCoder.Core.DTOs;

public class PagingRequest
{
    public int Page { get; set; } = PagingDefaults.DefaultPage;
    public int PageSize { get; set; } = PagingDefaults.DefaultPageSize;
}
