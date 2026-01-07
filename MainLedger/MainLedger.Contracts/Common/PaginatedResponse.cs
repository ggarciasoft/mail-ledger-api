namespace MainLedger.Contracts.Common;

/// <summary>
/// Generic paginated response wrapper.
/// </summary>
public class PaginatedResponse<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }

    public static PaginatedResponse<T> Create(
        List<T> items,
        int totalCount,
        int page,
        int pageSize)
    {
        return new PaginatedResponse<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
