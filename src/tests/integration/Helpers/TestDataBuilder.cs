using AuthService.Abstractions.Common;
using AuthService.Abstractions.Security;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Persistence.EfCore;

namespace AuthService.IntegrationTests.Helpers;

/// <summary>
/// Builder class for creating test data in the database
/// </summary>
public class TestDataBuilder
{
    private readonly IdentityDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TestDataBuilder(
        IdentityDbContext context,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Role> CreateRoleAsync(string roleCode, string description)
    {
        var role = Role.Create(roleCode, description, _dateTimeProvider.UtcNow);
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();
        return role;
    }

    public async Task<Permission> CreatePermissionAsync(string permCode, string description)
    {
        var permission = Permission.Create(permCode, description, _dateTimeProvider.UtcNow);
        await _context.Permissions.AddAsync(permission);
        await _context.SaveChangesAsync();
        return permission;
    }

    public async Task<RolePermission> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        var rolePermission = RolePermission.Create(roleId, permissionId, _dateTimeProvider.UtcNow);
        await _context.RolePermissions.AddAsync(rolePermission);
        await _context.SaveChangesAsync();
        return rolePermission;
    }

    public async Task<User> CreateUserAsync(UserType userType, bool activate = true)
    {
        var user = User.Create(userType, _dateTimeProvider.UtcNow);
        if (activate)
        {
            user.Activate(_dateTimeProvider.UtcNow);
        }
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<Contact> CreateContactAsync(Guid userId, string email, bool isPrimary = true, bool isVerified = true)
    {
        var contact = Contact.Create(userId, ContactKind.Email, email, isPrimary, _dateTimeProvider.UtcNow);
        if (isVerified)
        {
            contact.MarkAsVerified(_dateTimeProvider.UtcNow);
        }
        await _context.Contacts.AddAsync(contact);
        await _context.SaveChangesAsync();
        return contact;
    }

    public async Task<Credential> CreateCredentialAsync(Guid userId, string password, bool mustChange = false)
    {
        var passwordHash = _passwordHasher.HashPassword(password);
        var credential = Credential.Create(
            userId,
            passwordHash,
            MfaMode.PasswordAndOtp,
            mustChange,
            _dateTimeProvider.UtcNow);

        await _context.Credentials.AddAsync(credential);
        await _context.SaveChangesAsync();
        return credential;
    }

    public async Task<UserSchoolMembership> CreateMembershipAsync(
        Guid userId,
        Guid? schoolId,
        Guid roleId,
        bool activate = true)
    {
        var membership = UserSchoolMembership.Create(userId, schoolId, roleId, _dateTimeProvider.UtcNow);
        if (activate)
        {
            membership.Activate(_dateTimeProvider.UtcNow);
        }
        await _context.UserSchoolMemberships.AddAsync(membership);
        await _context.SaveChangesAsync();
        return membership;
    }

    public async Task<School> CreateSchoolAsync(string slug, string officialName, string location)
    {
        var school = School.Create(slug, officialName, $"EMIS-{slug}", location, _dateTimeProvider.UtcNow);
        school.Activate(_dateTimeProvider.UtcNow);
        await _context.Schools.AddAsync(school);
        await _context.SaveChangesAsync();
        return school;
    }

    public async Task<(User user, Contact contact, Credential credential, UserSchoolMembership membership)>
        CreateCompleteUserAsync(
            UserType userType,
            string email,
            string password,
            Guid roleId,
            Guid? schoolId = null)
    {
        var user = await CreateUserAsync(userType);
        var contact = await CreateContactAsync(user.Id, email);
        var credential = await CreateCredentialAsync(user.Id, password);
        var membership = await CreateMembershipAsync(user.Id, schoolId, roleId);

        return (user, contact, credential, membership);
    }
}

