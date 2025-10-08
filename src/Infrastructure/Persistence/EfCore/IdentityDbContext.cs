using Microsoft.EntityFrameworkCore;
using AuthService.Domain.Entities;

namespace AuthService.Infrastructure.Persistence.EfCore;

/// <summary>
/// Main database context for the Identity/Auth service
/// Handles all authentication, authorization, and school management data
/// </summary>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    // Identity schema
    public DbSet<User> Users => Set<User>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<Username> Usernames => Set<Username>();
    public DbSet<UserSchoolMembership> UserSchoolMemberships => Set<UserSchoolMembership>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // School schema
    public DbSet<School> Schools => Set<School>();

    // Student schema
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentIdentifier> StudentIdentifiers => Set<StudentIdentifier>();

    // Guardian schema
    public DbSet<GuardianLink> GuardianLinks => Set<GuardianLink>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureIdentitySchema(modelBuilder);
        ConfigureSchoolSchema(modelBuilder);
        ConfigureStudentSchema(modelBuilder);
        ConfigureGuardianSchema(modelBuilder);
    }

    private void ConfigureIdentitySchema(ModelBuilder modelBuilder)
    {
        // identity.Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "identity");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("UserId");
            
            entity.Property(e => e.Type)
                .HasColumnName("UserType")
                .HasMaxLength(20)
                .HasConversion<string>()  // Store enum as string
                .IsRequired();
            
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasConversion<string>()
                .IsRequired();
            
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnName("CreatedAt")
                .HasColumnType("datetime2(3)")
                .IsRequired();
            
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnName("UpdatedAt")
                .HasColumnType("datetime2(3)");

            // Indexes for performance
            entity.HasIndex(e => new { e.Type, e.Status });
            entity.HasIndex(e => e.CreatedAtUtc);

            // Ignore domain events (not persisted)
            entity.Ignore(e => e.DomainEvents);
        });

        // identity.Contacts
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.ToTable("Contacts", "identity");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("ContactId");
            
            entity.Property(e => e.Kind)
                .HasMaxLength(10)
                .HasConversion<string>()
                .IsRequired();
            
            entity.Property(e => e.Value)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.IsPrimary).IsRequired();
            entity.Property(e => e.IsVerified).IsRequired();
            
            entity.Property(e => e.VerifiedAt)
                .HasColumnType("datetime2(3)");
            
            entity.Property(e => e.CreatedAtUtc).HasColumnName("CreatedAt").HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

            // Foreign key to User
            entity.HasIndex(e => e.UserId);
            
            // Unique constraint: (UserId, Kind, Value)
            entity.HasIndex(e => new { e.UserId, e.Kind, e.Value }).IsUnique();
            
            // Index for lookups by value (email/phone login)
            entity.HasIndex(e => new { e.Value, e.Kind, e.IsVerified });

            entity.Ignore(e => e.DomainEvents);
        });

        // identity.Credentials
        modelBuilder.Entity<Credential>(entity =>
        {
            entity.ToTable("Credentials", "identity");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("CredentialId");
            
            entity.Property(e => e.PasswordHash)
                .HasColumnType("varbinary(max)")
                .IsRequired();
            
            entity.Property(e => e.MfaMode)
                .HasMaxLength(20)
                .HasConversion<string>()
                .IsRequired();
            
            entity.Property(e => e.TotpSecret).HasMaxLength(100);
            entity.Property(e => e.MustChangePassword).IsRequired();
            
            entity.Property(e => e.LastPasswordChangedAt).HasColumnType("datetime2(3)");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("CreatedAt").HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

            // One-to-one with User
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.Ignore(e => e.DomainEvents);
        });

        // identity.Usernames
        modelBuilder.Entity<Username>(entity =>
        {
            entity.ToTable("Usernames", "identity");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("UsernameId");
            
            entity.Property(e => e.Value)
                .HasColumnName("Username")
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.CreatedAtUtc).HasColumnName("CreatedAt").HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

            // Unique constraint: username must be unique per school
            entity.HasIndex(e => new { e.SchoolId, e.Value }).IsUnique();
            
            // Index for username lookup
            entity.HasIndex(e => e.Value);

            entity.Ignore(e => e.DomainEvents);
        });

        // identity.UserSchoolMemberships
        modelBuilder.Entity<UserSchoolMembership>(entity =>
        {
            entity.ToTable("UserSchoolMemberships", "identity");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("MembershipId");
            
            entity.Property(e => e.SchoolId).IsRequired(false); // Nullable for SystemAdmin
            
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasConversion<string>()
                .IsRequired();
            
            entity.Property(e => e.CreatedAtUtc).HasColumnName("CreatedAt").HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

            // Indexes for multi-tenant queries
            entity.HasIndex(e => new { e.UserId, e.SchoolId, e.Status });
            entity.HasIndex(e => new { e.SchoolId, e.RoleId, e.Status });

            // Unique: User can have a role in a school only once
            entity.HasIndex(e => new { e.UserId, e.SchoolId, e.RoleId }).IsUnique();

            entity.Ignore(e => e.DomainEvents);
        });

        // identity.RefreshTokens
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens", "identity");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("TokenId");
            
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.IssuedAt).HasColumnType("datetime2(3)").IsRequired();
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime2(3)").IsRequired();
            entity.Property(e => e.RevokedAt).HasColumnType("datetime2(3)");
            entity.Property(e => e.RevokedReason).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(45);  // IPv6 length
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            
            entity.Property(e => e.CreatedAtUtc).HasColumnName("CreatedAt").HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

            // Index for active token lookups
            entity.HasIndex(e => new { e.UserId, e.ExpiresAt, e.RevokedAt });
            entity.HasIndex(e => e.TokenHash).IsUnique();

            entity.Ignore(e => e.DomainEvents);
        });

        // identity.Roles
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles", "identity");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("RoleId");
            
            entity.Property(e => e.RoleCode)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsRequired();
            
            entity.Property(e => e.CreatedAtUtc).HasColumnName("CreatedAt").HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

            // Unique constraint on RoleCode
            entity.HasIndex(e => e.RoleCode).IsUnique();

            entity.Ignore(e => e.DomainEvents);
        });

        // identity.Permissions
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions", "identity");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("PermissionId");
            
            entity.Property(e => e.PermCode)
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsRequired();
            
            entity.Property(e => e.CreatedAtUtc).HasColumnName("CreatedAt").HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

            // Unique constraint on PermCode
            entity.HasIndex(e => e.PermCode).IsUnique();

            entity.Ignore(e => e.DomainEvents);
        });

        // identity.RolePermissions (many-to-many)
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions", "identity");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("RolePermissionId");
            
            entity.Property(e => e.CreatedAtUtc).HasColumnName("CreatedAt").HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

            // Foreign keys
            entity.HasOne(e => e.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: Role can have permission only once
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();

            entity.Ignore(e => e.DomainEvents);
        });
    }

    private void ConfigureSchoolSchema(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<School>(entity =>
        {
            entity.ToTable("Schools", "school");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("SchoolId");
            
            entity.Property(e => e.Slug)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.OfficialName)
                .HasMaxLength(200)
                .IsRequired();
            
            entity.Property(e => e.EmisCode).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(200).IsRequired();
            
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasConversion<string>()
                .IsRequired();
            
            entity.Property(e => e.CreatedAtUtc).HasColumnName("CreatedAt").HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

            // Unique constraints
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.EmisCode).IsUnique();
            
            // Index for active schools
            entity.HasIndex(e => e.Status);

            entity.Ignore(e => e.DomainEvents);
        });
    }

    private void ConfigureStudentSchema(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students", "student");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("StudentId");
            
            entity.Property(e => e.OfficialNumber).HasMaxLength(50);
            
            entity.Property(e => e.DateOfBirth)
                .HasColumnName("DoB")
                .HasColumnType("date")
                .IsRequired();
            
            entity.Property(e => e.CreatedAtUtc).HasColumnName("CreatedAt").HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

            // One-to-one with User
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<StudentIdentifier>(entity =>
        {
            entity.ToTable("StudentIdentifiers", "student");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Kind)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.Value)
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.CreatedAtUtc).HasColumnName("CreatedAt").HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

            // Unique: Admission number per school
            entity.HasIndex(e => new { e.SchoolId, e.Kind, e.Value }).IsUnique();
            
            // Index for lookups
            entity.HasIndex(e => e.StudentId);

            entity.Ignore(e => e.DomainEvents);
        });
    }

    private void ConfigureGuardianSchema(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GuardianLink>(entity =>
        {
            entity.ToTable("GuardianLinks", "guardian");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("GuardianLinkId");
            
            entity.Property(e => e.Relationship)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.VerifiedAt).HasColumnType("datetime2(3)");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("CreatedAt").HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("UpdatedAt").HasColumnType("datetime2(3)");

            // Indexes for parent and student queries
            entity.HasIndex(e => new { e.ParentUserId, e.Status });
            entity.HasIndex(e => new { e.StudentId, e.Status });
            entity.HasIndex(e => new { e.SchoolId, e.Status });

            // Unique: Parent can be linked to student only once
            entity.HasIndex(e => new { e.ParentUserId, e.StudentId }).IsUnique();

            entity.Ignore(e => e.DomainEvents);
        });
    }
}
