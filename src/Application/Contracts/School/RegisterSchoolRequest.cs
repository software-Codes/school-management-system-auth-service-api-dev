namespace AuthService.Application.Contracts.School;

public sealed record RegisterSchoolRequest
{
    public required string OfficialName { get; init; }
    public required string Slug { get; init; }
    public required string EmisCode { get; init; }
    public required string Location { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
}