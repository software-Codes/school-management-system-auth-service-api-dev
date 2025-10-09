namespace AuthService.Application.Contracts.School;

/// <summary>
/// Request for partially updating school attributes
/// All fields are optional - only provided fields will be updated
/// </summary>
public sealed record PatchSchoolRequest
{
    public string? OfficialName { get; init; }
    public string? Location { get; init; }
    public string? EmisCode { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? Status { get; init; } // "Active", "Suspended", "Closed"
}

