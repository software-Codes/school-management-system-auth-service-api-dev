namespace AuthService.Application.Contracts.School;

public sealed record UpdateSchoolRequest
{
    public required string OfficialName { get; init; }
    public required string Location { get; init; }
    public string? EmisCode { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
}