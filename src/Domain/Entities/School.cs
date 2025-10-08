using AuthService.Domain.Common;
using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities;


public class School : BaseEntity
{
    private School() { }

    public static School Create(
        string slug,
        string officialName,
        string emisCode,
        string location,
        DateTime utcNow,
        string? email = null,
        string? phone = null,
        string? address = null)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("School slug cannot be empty", nameof(slug));
        if (string.IsNullOrWhiteSpace(officialName))
            throw new ArgumentException("School name cannot be empty", nameof(officialName));

        return new School
        {
            Id = Guid.NewGuid(),
            Slug = slug.Trim().ToLowerInvariant(),
            OfficialName = officialName.Trim(),
            EmisCode = emisCode?.Trim(),
            Location = location,
            Email = email,
            Phone = phone,
            Address = address,
            Status = SchoolStatus.Active,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public string Slug { get; private set; } = null!;  // URL-friendly: "meruschool"
    public string OfficialName { get; private set; } = null!;
    public string? EmisCode { get; private set; }  // Government education code
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string Location { get; private set; } = null!;
    public SchoolStatus Status { get; private set; }

    public void Activate(DateTime utcNow)
    {
        Status = SchoolStatus.Active;
        MarkAsUpdated(utcNow);
    }

    public void Suspend(DateTime utcNow)
    {
        Status = SchoolStatus.Suspended;
        MarkAsUpdated(utcNow);
    }

    public void Close(DateTime utcNow)
    {
        Status = SchoolStatus.Closed;
        MarkAsUpdated(utcNow);
    }

    public void UpdateContactInfo(string? email, string? phone, DateTime utcNow)
    {
        Email = email;
        Phone = phone;
        MarkAsUpdated(utcNow);
    }
}
