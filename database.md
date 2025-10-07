/* ============================================
   0) PREP: convenience shorthands
   ============================================ */
DECLARE @now DATETIME2(3) = SYSUTCDATETIME();

/* ============================================
   1) Roles & Permissions (minimal)
   ============================================ */
-- Roles
INSERT INTO identity.Roles (RoleCode, Description) VALUES
 ('SystemAdmin','Platform super administrator'),
 ('Principal','School principal'),
 ('DeputyPrincipal','Deputy principal'),
 ('Teacher','Teacher'),
 ('Accountant','Finance/Accountant'),
 ('InfoDesk','Secretarial/Information Desk'),
 ('TransportManager','Transport manager'),
 ('Student','Student'),
 ('Parent','Parent/Guardian');

-- Permissions (extend later)
INSERT INTO identity.Permissions (PermCode, Description) VALUES
 ('school.manage','Create schools, assign principals'),
 ('staff.invite','Invite school staff'),
 ('student.read','View student profiles'),
 ('student.manage','Create/modify student profiles'),
 ('enrollment.read','View enrollments'),
 ('finance.read','View fee balances'),
 ('finance.post','Record payments'),
 ('grades.read','View grades'),
 ('grades.post','Post/modify grades'),
 ('attendance.post','Mark attendance');

-- Role → Permission
-- SystemAdmin: everything (sample subset here)
INSERT INTO identity.RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM identity.Roles r CROSS JOIN identity.Permissions p
WHERE r.RoleCode = 'SystemAdmin';

-- Principal
INSERT INTO identity.RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM identity.Roles r
JOIN identity.Permissions p ON p.PermCode IN
 ('staff.invite','student.read','student.manage','enrollment.read','grades.read','finance.read')
WHERE r.RoleCode = 'Principal';

-- Teacher
INSERT INTO identity.RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM identity.Roles r
JOIN identity.Permissions p ON p.PermCode IN
 ('student.read','grades.read','grades.post','attendance.post','enrollment.read')
WHERE r.RoleCode = 'Teacher';

-- Accountant
INSERT INTO identity.RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM identity.Roles r
JOIN identity.Permissions p ON p.PermCode IN
 ('finance.read','finance.post','student.read')
WHERE r.RoleCode = 'Accountant';

-- Parent
INSERT INTO identity.RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM identity.Roles r
JOIN identity.Permissions p ON p.PermCode IN
 ('student.read','grades.read','finance.read','enrollment.read')
WHERE r.RoleCode = 'Parent';

/* ============================================
   2) School (tenant) + Classes
   ============================================ */
DECLARE @SchoolId UNIQUEIDENTIFIER = NEWID();

INSERT INTO school.Schools (SchoolId, Slug, OfficialName, EmisCode, Email, Phone, Address, Location, Status, CreatedAt)
VALUES (@SchoolId, 'meruschool', N'Meru School', 'EMIS-KE-0001', 'info@meruschool.ac.ke', '+254700000001',
        N'Nanyuki Rd, Meru', N'Meru, Kenya', 'Active', @now);

-- Classes (example: Grade 7, Stream A, 2025)
DECLARE @ClassId UNIQUEIDENTIFIER = NEWID();
INSERT INTO school.Classes (ClassId, SchoolId, Level, Stream, Year)
VALUES (@ClassId, @SchoolId, 'Grade7', 'A', 2025);

/* ============================================
   3) Users: SystemAdmin, Principal, Student, Parent
   ============================================ */
-- System Admin
DECLARE @SysId UNIQUEIDENTIFIER = NEWID();
INSERT INTO identity.Users (UserId, UserType, Status, CreatedAt, UpdatedAt)
VALUES (@SysId, 'SystemAdmin', 'Active', @now, @now);

INSERT INTO identity.Contacts (ContactId, UserId, Kind, Value, IsPrimary, IsVerified, VerifiedAt)
VALUES (NEWID(), @SysId, 'Email', 'admin@platform.local', 1, 1, @now);

-- Credentials (placeholder hash)
INSERT INTO identity.Credentials (UserId, PasswordHash, MfaMode, MustChangePassword)
VALUES (@SysId, 0x01, 'pass_otp', 0);

-- Principal
DECLARE @PrincipalId UNIQUEIDENTIFIER = NEWID();
INSERT INTO identity.Users (UserId, UserType, Status, CreatedAt, UpdatedAt, CreatedBy)
VALUES (@PrincipalId, 'Principal', 'Active', @now, @now, @SysId);

INSERT INTO identity.Contacts (ContactId, UserId, Kind, Value, IsPrimary, IsVerified, VerifiedAt)
VALUES (NEWID(), @PrincipalId, 'Email', 'principal@meruschool.ac.ke', 1, 1, @now);

-- Principal membership
INSERT INTO identity.UserSchoolMemberships (MembershipId, UserId, SchoolId, RoleId, Status, CreatedAt, CreatedBy)
SELECT NEWID(), @PrincipalId, @SchoolId, RoleId, 'Active', @now, @SysId
FROM identity.Roles WHERE RoleCode = 'Principal';

-- Principal username (for the school)
INSERT INTO identity.Usernames (UsernameId, UserId, SchoolId, Username, IsPrimary)
VALUES (NEWID(), @PrincipalId, @SchoolId, 'principal@meruschool', 1);

-- Principal credentials (placeholder hash; enforce TOTP later)
INSERT INTO identity.Credentials (UserId, PasswordHash, MfaMode, MustChangePassword)
VALUES (@PrincipalId, 0x02, 'pass_otp', 1);

/* ============================================
   4) Student user + profile + enrollment + username
   ============================================ */
DECLARE @StudentUserId UNIQUEIDENTIFIER = NEWID();
DECLARE @StudentId UNIQUEIDENTIFIER = NEWID();

-- User
INSERT INTO identity.Users (UserId, UserType, Status, CreatedAt, UpdatedAt, CreatedBy)
VALUES (@StudentUserId, 'Student', 'Active', @now, @now, @PrincipalId);

-- Username rule: {admission}@{school_slug}
-- Admission/UPI example: CT201
INSERT INTO identity.Usernames (UsernameId, UserId, SchoolId, Username, IsPrimary)
VALUES (NEWID(), @StudentUserId, @SchoolId, 'ct201@meruschool', 1);

-- Membership (Student)
INSERT INTO identity.UserSchoolMemberships (MembershipId, UserId, SchoolId, RoleId, Status, CreatedAt, CreatedBy)
SELECT NEWID(), @StudentUserId, @SchoolId, RoleId, 'Active', @now, @PrincipalId
FROM identity.Roles WHERE RoleCode = 'Student';

-- Student profile
INSERT INTO student.Students (StudentId, UserId, OfficialNumber, DateOfBirth, Gender)
VALUES (@StudentId, @StudentUserId, 'CT201', '2012-03-14', 'M');

-- Identifier mapped to school
INSERT INTO student.StudentIdentifiers (Id, StudentId, SchoolId, Kind, Value, IsPrimary)
VALUES (NEWID(), @StudentId, @SchoolId, 'Admission', 'CT201', 1);

-- Enrollment (2025 Grade7A)
DECLARE @EnrollmentId UNIQUEIDENTIFIER = NEWID();
INSERT INTO student.Enrollments (EnrollmentId, StudentId, SchoolId, ClassId, Year, Status)
VALUES (@EnrollmentId, @StudentId, @SchoolId, @ClassId, 2025, 'Active');

-- (Optional) Student join token for self-onboarding (single use)
INSERT INTO student.JoinTokens (TokenId, EnrollmentId, ShortCode, ExpiresAt, UsesRemaining, IssuedBy)
VALUES (NEWID(), @EnrollmentId, 'MRS-7A-1', DATEADD(DAY, 7, @now), 1, @PrincipalId);

-- Student credentials
-- TEMP: password = admission number (CT201) -> force change on first login
INSERT INTO identity.Credentials (UserId, PasswordHash, MfaMode, MustChangePassword)
VALUES (@StudentUserId, 0x03, 'pass_only', 1);

/* ============================================
   5) Parent (OTP-focused) + Guardian Link to the student
   ============================================ */
DECLARE @ParentUserId UNIQUEIDENTIFIER = NEWID();

INSERT INTO identity.Users (UserId, UserType, Status, CreatedAt, UpdatedAt)
VALUES (@ParentUserId, 'Parent', 'Active', @now, @now);

-- Parent primary contact = phone (E.164), verified
INSERT INTO identity.Contacts (ContactId, UserId, Kind, Value, IsPrimary, IsVerified, VerifiedAt)
VALUES (NEWID(), @ParentUserId, 'Phone', '+254711222333', 1, 1, @now);

-- Optional email
INSERT INTO identity.Contacts (ContactId, UserId, Kind, Value, IsPrimary, IsVerified)
VALUES (NEWID(), @ParentUserId, 'Email', 'parent.ct201@example.com', 0, 0);

-- Parent membership (role = Parent) scoped to Meru School
INSERT INTO identity.UserSchoolMemberships (MembershipId, UserId, SchoolId, RoleId, Status, CreatedAt, CreatedBy)
SELECT NEWID(), @ParentUserId, @SchoolId, RoleId, 'Active', @now, @PrincipalId
FROM identity.Roles WHERE RoleCode = 'Parent';

-- Parent credentials
-- OPTION A (OTP-only): PasswordHash = NULL, MfaMode = 'otp_only'
INSERT INTO identity.Credentials (UserId, PasswordHash, MfaMode, MustChangePassword)
VALUES (@ParentUserId, NULL, 'otp_only', 0);

-- Parent ↔ Student link
INSERT INTO guardian.GuardianLinks (GuardianLinkId, ParentUserId, StudentId, SchoolId, Relationship, VerifiedAt, Status)
VALUES (NEWID(), @ParentUserId, @StudentId, @SchoolId, 'Father', @now, 'Active');

/* ============================================
   6) Example Staff Invite (Teacher) issued by Principal
   ============================================ */
DECLARE @TeacherInviteId UNIQUEIDENTIFIER = NEWID();
INSERT INTO invite.Invitations
 (InviteId, SchoolId, RoleId, TargetContact, PayloadJson, IssuedBy, FirstLoginUrlToken, ExpiresAt, Status)
SELECT
 @TeacherInviteId, @SchoolId, r.RoleId, 'teacher1@meruschool.ac.ke',
 N'{}', @PrincipalId, 'INV-TEACH-001', DATEADD(DAY, 10, @now), 'Issued'
FROM identity.Roles r WHERE r.RoleCode = 'Teacher';

/* ============================================
   7) Minimal Audit & Outbox samples
   ============================================ */
INSERT INTO identity.AuditLog (ActorUserId, Action, TargetType, TargetId, SchoolId, Ip, UserAgent, WhenAt, Metadata)
VALUES (@SysId, 'SchoolCreated', 'School', CONVERT(varchar(36),@SchoolId), @SchoolId, '127.0.0.1', 'seed-script', @now, N'{}');

INSERT INTO outbox.IntegrationEvents (EventId, Topic, AggregateType, AggregateId, OccurredAt, PayloadJson, PublishedAt, PublishAttempts)
VALUES (NEWID(), 'identity.user.created', 'User', CONVERT(varchar(36), @PrincipalId), @now, N'{"userType":"Principal"}', NULL, 0);
