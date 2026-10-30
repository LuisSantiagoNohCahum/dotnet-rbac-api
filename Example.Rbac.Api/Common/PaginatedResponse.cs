namespace Example.Rbac.Api.Common;

public sealed class PaginatedResponse<T>
{
    public IReadOnlyCollection<T>? Items { get; set; }
    public int TotalCount { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}
