using AuthService.Domain.Common;
using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities;


public class UserSchoolMembership : BaseEntity
{
    private UserSchoolMembership() { }

    public static UserSchoolMembership Create(
        Guid userId, 
        Guid schoolId, 
        Guid roleId, 
        DateTime utcNow)
    {
        return new UserSchoolMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SchoolId = schoolId,
            RoleId = roleId,
            Status = MembershipStatus.Active,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public Guid UserId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid RoleId { get; private set; }
    public MembershipStatus Status { get; private set; }

    public void Disable(DateTime utcNow)
    {
        if (Status == MembershipStatus.Disabled) return;
        
        Status = MembershipStatus.Disabled;
        MarkAsUpdated(utcNow);
    }

    public void Activate(DateTime utcNow)
    {
        if (Status == MembershipStatus.Active) return;
        
        Status = MembershipStatus.Active;
        MarkAsUpdated(utcNow);
    }
}
