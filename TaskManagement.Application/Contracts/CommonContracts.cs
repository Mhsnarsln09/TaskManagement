namespace TaskManagement.Application.Contracts;

public sealed record PageQuery(int Page = 1, int PageSize = 20);

public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
