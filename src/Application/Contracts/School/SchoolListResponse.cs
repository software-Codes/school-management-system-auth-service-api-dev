namespace AuthService.Application.Contracts.School;

public sealed record SchoolListResponse

{
    public List<SchoolResponse> Schools { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}