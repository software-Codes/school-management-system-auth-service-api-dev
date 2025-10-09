using AuthService.Infrastructure.Persistence.Seeding.Models;

namespace AuthService.Infrastructure.Persistence.Seeding.Data;

/// <summary>
/// Single source of truth for all permissions in the system
/// Following CUPID: Composable, Unix philosophy (do one thing well)
/// </summary>
public static class PermissionsData
{
    public static IReadOnlyList<PermissionSeedData> GetAll() => new List<PermissionSeedData>
    {
        // School Management
        new() { PermCode = "school.manage", Description = "Create schools, assign principals" },
        new() { PermCode = "school.read", Description = "View school information" },
        new() { PermCode = "school.update", Description = "Update school information" },
        
        // Staff Management
        new() { PermCode = "staff.invite", Description = "Invite school staff" },
        new() { PermCode = "staff.read", Description = "View staff profiles" },
        new() { PermCode = "staff.manage", Description = "Manage staff profiles and permissions" },
        
        // Student Management
        new() { PermCode = "student.read", Description = "View student profiles" },
        new() { PermCode = "student.manage", Description = "Create/modify student profiles" },
        new() { PermCode = "student.delete", Description = "Delete student profiles" },
        
        // Enrollment
        new() { PermCode = "enrollment.read", Description = "View enrollments" },
        new() { PermCode = "enrollment.manage", Description = "Create/modify enrollments" },
        
        // Finance
        new() { PermCode = "finance.read", Description = "View fee balances and transactions" },
        new() { PermCode = "finance.post", Description = "Record payments and fees" },
        new() { PermCode = "finance.manage", Description = "Manage fee structures" },
        
        // Grades & Assessment
        new() { PermCode = "grades.read", Description = "View grades" },
        new() { PermCode = "grades.post", Description = "Post/modify grades" },
        new() { PermCode = "grades.approve", Description = "Approve and publish grades" },
        
        // Attendance
        new() { PermCode = "attendance.read", Description = "View attendance records" },
        new() { PermCode = "attendance.post", Description = "Mark attendance" },
        
        // Classes & Curriculum
        new() { PermCode = "class.read", Description = "View class information" },
        new() { PermCode = "class.manage", Description = "Create/modify classes" },
        
        // Transport
        new() { PermCode = "transport.read", Description = "View transport routes and logs" },
        new() { PermCode = "transport.manage", Description = "Manage transport routes and assignments" },
        
        // Communication
        new() { PermCode = "communication.send", Description = "Send notifications and messages" },
        new() { PermCode = "communication.read", Description = "View communication history" },
        
        // Reports & Analytics
        new() { PermCode = "reports.view", Description = "View system reports" },
        new() { PermCode = "reports.generate", Description = "Generate custom reports" },
        
        // User Management (Admin)
        new() { PermCode = "user.read", Description = "View all user accounts" },
        new() { PermCode = "user.manage", Description = "Create/modify user accounts" },
        new() { PermCode = "user.delete", Description = "Delete user accounts" },
        new() { PermCode = "users.create", Description = "Create new user accounts" },
        new() { PermCode = "users.read", Description = "View user accounts" },
        new() { PermCode = "users.update", Description = "Update user accounts" },
        new() { PermCode = "users.delete", Description = "Delete user accounts" },
        
        // System Administration
        new() { PermCode = "system.admin", Description = "Full system administration access" },
        new() { PermCode = "audit.read", Description = "View audit logs" },
    };
}

