using AuthService.Infrastructure.Persistence.Seeding.Models;

namespace AuthService.Infrastructure.Persistence.Seeding.Data;

/// <summary>
/// Single source of truth for all roles in the system
/// Following CUPID: Composable, Unix philosophy (do one thing well)
/// </summary>
public static class RolesData
{
    public static IReadOnlyList<RoleSeedData> GetAll() => new List<RoleSeedData>
    {
        new()
        {
            RoleCode = "SystemAdmin",
            Description = "Platform super administrator with full access",
            PermissionCodes = new List<string>
            {
                // System Admin has ALL permissions
                "system.admin",
                "school.manage",
                "school.read",
                "school.update",
                "staff.invite",
                "staff.read",
                "staff.manage",
                "student.read",
                "student.manage",
                "student.delete",
                "enrollment.read",
                "enrollment.manage",
                "finance.read",
                "finance.post",
                "finance.manage",
                "grades.read",
                "grades.post",
                "grades.approve",
                "attendance.read",
                "attendance.post",
                "class.read",
                "class.manage",
                "transport.read",
                "transport.manage",
                "communication.send",
                "communication.read",
                "reports.view",
                "reports.generate",
                "user.read",
                "user.manage",
                "user.delete",
                "audit.read"
            }
        },
        
        new()
        {
            RoleCode = "Principal",
            Description = "School principal with full school management access",
            PermissionCodes = new List<string>
            {
                "school.read",
                "school.update",
                "staff.invite",
                "staff.read",
                "staff.manage",
                "student.read",
                "student.manage",
                "enrollment.read",
                "enrollment.manage",
                "finance.read",
                "finance.manage",
                "grades.read",
                "grades.approve",
                "attendance.read",
                "class.read",
                "class.manage",
                "transport.read",
                "transport.manage",
                "communication.send",
                "communication.read",
                "reports.view",
                "reports.generate"
            }
        },
        
        new()
        {
            RoleCode = "DeputyPrincipal",
            Description = "Deputy principal with extended school management access",
            PermissionCodes = new List<string>
            {
                "school.read",
                "staff.invite",
                "staff.read",
                "student.read",
                "student.manage",
                "enrollment.read",
                "enrollment.manage",
                "finance.read",
                "grades.read",
                "grades.approve",
                "attendance.read",
                "class.read",
                "class.manage",
                "communication.send",
                "communication.read",
                "reports.view",
                "reports.generate"
            }
        },
        
        new()
        {
            RoleCode = "Teacher",
            Description = "Teacher with classroom management access",
            PermissionCodes = new List<string>
            {
                "student.read",
                "enrollment.read",
                "grades.read",
                "grades.post",
                "attendance.read",
                "attendance.post",
                "class.read",
                "communication.send",
                "communication.read",
                "reports.view"
            }
        },
        
        new()
        {
            RoleCode = "Accountant",
            Description = "Finance/Accountant with financial management access",
            PermissionCodes = new List<string>
            {
                "student.read",
                "finance.read",
                "finance.post",
                "finance.manage",
                "reports.view",
                "reports.generate"
            }
        },
        
        new()
        {
            RoleCode = "InfoDesk",
            Description = "Secretarial/Information Desk with administrative support access",
            PermissionCodes = new List<string>
            {
                "school.read",
                "student.read",
                "enrollment.read",
                "communication.send",
                "communication.read",
                "reports.view"
            }
        },
        
        new()
        {
            RoleCode = "TransportManager",
            Description = "Transport manager with transport operations access",
            PermissionCodes = new List<string>
            {
                "student.read",
                "transport.read",
                "transport.manage",
                "communication.send",
                "reports.view"
            }
        },
        
        new()
        {
            RoleCode = "Student",
            Description = "Student with limited read-only access to own data",
            PermissionCodes = new List<string>
            {
                "grades.read",
                "attendance.read",
                "class.read",
                "communication.read"
            }
        },
        
        new()
        {
            RoleCode = "Parent",
            Description = "Parent/Guardian with read-only access to child's data",
            PermissionCodes = new List<string>
            {
                "student.read",
                "enrollment.read",
                "grades.read",
                "finance.read",
                "attendance.read",
                "communication.read"
            }
        }
    };
}

