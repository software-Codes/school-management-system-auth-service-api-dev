using AuthService.Infrastructure.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IntegrationTests.Helpers;

/// <summary>
/// Test-specific DbContext that enforces unique constraints for integration tests
/// </summary>
public class TestIdentityDbContext : IdentityDbContext
{
    public TestIdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Manually enforce unique constraints that InMemory provider doesn't enforce
        await EnforceUniqueConstraintsAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task EnforceUniqueConstraintsAsync(CancellationToken cancellationToken)
    {
        // Check Contact unique constraint: (UserId, Kind, Value)
        var contactGroups = ChangeTracker.Entries<Domain.Entities.Contact>()
            .Where(e => e.State == EntityState.Added)
            .GroupBy(e => new { e.Entity.UserId, e.Entity.Kind, e.Entity.Value })
            .Where(g => g.Count() > 1);

        if (contactGroups.Any())
        {
            throw new DbUpdateException("Duplicate contact entries detected");
        }

        // Check for existing contacts with same (UserId, Kind, Value)
        foreach (var entry in ChangeTracker.Entries<Domain.Entities.Contact>()
            .Where(e => e.State == EntityState.Added))
        {
            var exists = await Contacts.AnyAsync(c => 
                c.UserId == entry.Entity.UserId && 
                c.Kind == entry.Entity.Kind && 
                c.Value == entry.Entity.Value, cancellationToken);
            
            if (exists)
            {
                throw new DbUpdateException("Contact with same UserId, Kind, and Value already exists");
            }
        }

        // Check Role unique constraint: RoleCode
        var roleGroups = ChangeTracker.Entries<Domain.Entities.Role>()
            .Where(e => e.State == EntityState.Added)
            .GroupBy(e => e.Entity.RoleCode)
            .Where(g => g.Count() > 1);

        if (roleGroups.Any())
        {
            throw new DbUpdateException("Duplicate role codes detected");
        }

        foreach (var entry in ChangeTracker.Entries<Domain.Entities.Role>()
            .Where(e => e.State == EntityState.Added))
        {
            var exists = await Roles.AnyAsync(r => r.RoleCode == entry.Entity.RoleCode, cancellationToken);
            if (exists)
            {
                throw new DbUpdateException("Role with same code already exists");
            }
        }

        // Check Permission unique constraint: PermCode
        var permissionGroups = ChangeTracker.Entries<Domain.Entities.Permission>()
            .Where(e => e.State == EntityState.Added)
            .GroupBy(e => e.Entity.PermCode)
            .Where(g => g.Count() > 1);

        if (permissionGroups.Any())
        {
            throw new DbUpdateException("Duplicate permission codes detected");
        }

        foreach (var entry in ChangeTracker.Entries<Domain.Entities.Permission>()
            .Where(e => e.State == EntityState.Added))
        {
            var exists = await Permissions.AnyAsync(p => p.PermCode == entry.Entity.PermCode, cancellationToken);
            if (exists)
            {
                throw new DbUpdateException("Permission with same code already exists");
            }
        }

        // Check UserSchoolMembership unique constraint: (UserId, SchoolId, RoleId)
        var membershipGroups = ChangeTracker.Entries<Domain.Entities.UserSchoolMembership>()
            .Where(e => e.State == EntityState.Added)
            .GroupBy(e => new { e.Entity.UserId, e.Entity.SchoolId, e.Entity.RoleId })
            .Where(g => g.Count() > 1);

        if (membershipGroups.Any())
        {
            throw new DbUpdateException("Duplicate user school memberships detected");
        }

        foreach (var entry in ChangeTracker.Entries<Domain.Entities.UserSchoolMembership>()
            .Where(e => e.State == EntityState.Added))
        {
            var exists = await UserSchoolMemberships.AnyAsync(m => 
                m.UserId == entry.Entity.UserId && 
                m.SchoolId == entry.Entity.SchoolId && 
                m.RoleId == entry.Entity.RoleId, cancellationToken);
            
            if (exists)
            {
                throw new DbUpdateException("User school membership already exists");
            }
        }
    }
}