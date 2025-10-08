# 🔐 Authentication Flow Design - School Management System

## 🎯 **Design Principles**

1. **Zero Trust** - Every user must be explicitly granted access
2. **Multi-Tenant Isolation** - Users can only see/access their school's data
3. **Data Protection** - Parents use OTP to protect student confidentiality
4. **Hierarchical Control** - Clear chain of account creation authority
5. **Audit Trail** - Every login and account creation is logged

---

## 👥 **User Hierarchy & Account Creation**

```
System Admin (Super User)
    ↓ creates
Principal / Deputy Principal (per school)
    ↓ creates
Teachers, Accountants, Info Desk, Transport Managers
    ↓ (no creation rights)
Students (self-register with verification)
Parents (self-register linked to student with OTP verification)
```

---

## 🏫 **School Registration (System Admin Only)**

### Prerequisites
- Must be System Admin user type

### Process
1. System Admin creates school record
   - School name (official)
   - School slug (URL-friendly, unique: `meruschool`)
   - EMIS code (government identifier)
   - Location, contact info
2. System Admin creates Principal account
3. Assigns Principal to school

### Database Impact
- `school.Schools` table - new record
- `identity.Users` - Principal user created
- `identity.UserSchoolMemberships` - Principal assigned to school

---

## 🎓 **Student Registration (Self-Service with Validation)**

### Registration Form
```
First Name:        [John]
Middle Name:       [Kamau]  (optional)
Surname:           [Mwangi]
Admission Number:  [CT201]  or UPI: [12345678]
School Name:       [Select from dropdown - Meru School]
Class/Grade:       [Select - Grade 7, Stream A]
Date of Birth:     [2010-03-15]
Password:          [********]
```

### Validation Rules
1. ✅ School must be selected from registered schools (dropdown)
2. ✅ Class/Grade must be selected
3. ✅ Admission number must be unique within school
4. ✅ Date of birth reasonable (age 4-25)
5. ✅ Password strength requirements (min 8 chars, contains number/letter)
6. ✅ Admission number format validation (alphanumeric, max 20 chars)

### Process Flow
```
Student fills form
    ↓
Validate school exists
    ↓
Check admission# not already registered
    ↓
Create User (type: Student, status: Pending)
    ↓
Create StudentProfile (linked to User)
    ↓
Create StudentIdentifier (admission# or UPI)
    ↓
Generate username: {admissionNo}@{schoolSlug}
    ↓
Create Username record
    ↓
Hash password → Create Credential
    ↓
Send confirmation (if email/phone provided)
    ↓
Set status: Active
    ↓
⚠️ Flag: MustChangePassword = true (if password = admission#)
```

### Generated Username
- Format: `{admissionNo}@{schoolSlug}`
- Example: `CT201@meruschool`
- Unique per school
- Easy to remember

### Database Impact
- `identity.Users` - new student user
- `student.Students` - student profile
- `student.StudentIdentifiers` - admission#/UPI
- `identity.Usernames` - generated username
- `identity.Credentials` - hashed password

---

## 👨‍👩‍👧 **Parent Registration (Self-Service with OTP Verification)**

### Registration Form (Step 1 - Parent Info)
```
First Name:        [Jane]
Middle Name:       [Wanjiru]  (optional)
Surname:           [Mwangi]
Email:             [jane@example.com]  (optional but recommended)
Phone Number:      [+254712345678]     (required)
Password:          [********]
Confirm Password:  [********]
```

### Registration Form (Step 2 - Student Verification)
```
Student Information (to link your child):
School:            [Search by name or code...]
Student Surname:   [Mwangi]
Admission Number:  [CT201]  or UPI
Date of Birth:     [2010-03-15]
Relationship:      [Mother / Father / Guardian / Other]
```

### Validation Rules
1. ✅ Email OR phone required (at least one)
2. ✅ Phone number format validation (+254...)
3. ✅ Student must exist in system
4. ✅ Match: Admission# + DOB + School (for privacy)
5. ✅ Parent not already linked to same student
6. ✅ OTP verification on phone/email

### Process Flow
```
Parent fills Step 1 (personal info)
    ↓
Create User (type: Parent, status: Pending)
    ↓
Create Contact (email/phone, unverified)
    ↓
Hash password → Create Credential (MfaMode: otp_required)
    ↓
Parent fills Step 2 (student info)
    ↓
Validate student exists:
  - School + AdmissionNo + DOB match
    ↓
Create GuardianLink (status: PendingVerification)
    ↓
Send OTP to parent phone/email
    ↓
Parent enters OTP
    ↓
Verify OTP → Update Contact.IsVerified = true
    ↓
Update GuardianLink.Status = Active
    ↓
Update User.Status = Active
    ↓
✅ Registration complete
```

### Why This Is Secure
- 🔒 Can't see all students (must know admission# + DOB)
- 🔒 OTP verification prevents fake accounts
- 🔒 One student linked initially (prevents fishing)
- 🔒 Can add more students later (after verification)

### Database Impact
- `identity.Users` - parent user
- `identity.Contacts` - phone/email with verification
- `identity.Credentials` - password + MFA mode
- `guardian.GuardianLinks` - parent-student relationship

---

## 👨‍🏫 **Staff Registration (Admin-Created Only)**

### Who Can Create Staff?
- **Principal / Deputy Principal** can create:
  - Teachers
  - Accountants
  - Info Desk
  - Transport Managers

### Process (Invitation-Based)
```
Principal sends invitation
    ↓
Create User (status: Invited)
    ↓
Create Contact (email)
    ↓
Create Invitation record with:
  - Unique token
  - Target email
  - Role
  - Expiry (7 days)
    ↓
Send email with setup link:
  https://portal.school.com/setup/{token}
    ↓
Staff clicks link
    ↓
Validates token not expired
    ↓
Staff sets password
    ↓
Create Credential
    ↓
Create UserSchoolMembership (role assigned)
    ↓
Update User.Status = Active
    ↓
✅ Account activated
```

### Database Impact
- `identity.Users` - staff user
- `invite.Invitations` - invitation record
- `identity.Contacts` - email
- `identity.Credentials` - password
- `identity.UserSchoolMemberships` - role assignment

---

## 🔑 **Login Flows**

### 1. Student Login
```
Input: Username (ct201@meruschool) + Password
    ↓
Look up Username → get UserId
    ↓
Validate password hash
    ↓
Check status = Active
    ↓
Check MustChangePassword flag
    ↓ (if true)
Redirect to change password
    ↓ (else)
Generate JWT token
    ↓
Create RefreshToken
    ↓
Log LoginAttempt (success)
    ↓
Return tokens + user claims
```

### 2. Parent Login (OTP Required)
```
Input: Email/Phone + Password
    ↓
Look up Contact → get UserId
    ↓
Validate password hash
    ↓
Check status = Active
    ↓
Check MfaMode = otp_required
    ↓
Generate OTP (6 digits)
    ↓
Store in Redis (expire: 5 min)
    ↓
Send via Twilio (SMS/Email)
    ↓
Create OtpRequest record
    ↓
Return: "OTP sent, please verify"
    ↓
---
Parent enters OTP
    ↓
Validate OTP from Redis
    ↓
Check attempt count < 3
    ↓
Match OTP
    ↓
Generate JWT token (include guardian links)
    ↓
Create RefreshToken
    ↓
Update OtpRequest.Verified = true
    ↓
Log LoginAttempt (success)
    ↓
Return tokens + user claims
```

### 3. Staff/Admin Login
```
Input: Email + Password (+ optional TOTP)
    ↓
Look up Contact → get UserId
    ↓
Validate password hash
    ↓
Check status = Active, not Locked
    ↓
If MfaMode includes TOTP:
  Verify TOTP code
    ↓
Get UserSchoolMemberships → roles
    ↓
Generate JWT with:
  - UserId
  - UserType
  - School memberships
  - Permissions (from role)
    ↓
Create RefreshToken
    ↓
Log LoginAttempt (success)
    ↓
Return tokens + user claims
```

---

## 🛡️ **Security Features**

### Password Policy
- Minimum 8 characters
- Must include: uppercase, lowercase, number
- Cannot be common passwords
- Hashed with Argon2id (or bcrypt)
- Must change if default (admission number)

### OTP Security
- 6-digit code
- 5-minute expiry
- Max 3 attempts
- Rate limit: 1 OTP per minute per user
- Store in Redis (ephemeral)

### Account Lockout
- 5 failed login attempts → lock account
- Unlock: Admin action or 30-minute cooldown
- All attempts logged in `LoginAttempts`

### Token Security
- **Access Token (JWT):**
  - Short-lived (15 minutes)
  - Contains: UserId, UserType, SchoolIds, Roles
  - Signed with HMAC-SHA256
- **Refresh Token:**
  - Long-lived (30 days)
  - Stored in database with family chain
  - Rotated on each use
  - Revokable

---

## 📊 **Database Tables Used**

### Core Identity
- `identity.Users` - All users
- `identity.Contacts` - Emails/phones
- `identity.Credentials` - Passwords, MFA
- `identity.Usernames` - School-scoped usernames
- `identity.UserSchoolMemberships` - Roles per school
- `identity.RefreshTokens` - Session management
- `identity.LoginAttempts` - Audit log

### School Management
- `school.Schools` - School registry
- `school.Classes` - Classes per school

### Student-Specific
- `student.Students` - Student profiles
- `student.StudentIdentifiers` - Admission numbers
- `student.Enrollments` - School/class assignments

### Parent-Specific
- `guardian.GuardianLinks` - Parent-student relationships

### Invitations
- `invite.Invitations` - Staff invitation tracking

### Notifications
- `notify.OtpRequests` - OTP history

---

## ✅ **Improvements Made to Your Design**

| Your Idea | Improvement | Why |
|-----------|-------------|-----|
| School name dropdown | Searchable dropdown (active schools only) | User-friendly for students |
| Confirm password field | Removed for students | Simplified UX, validated server-side |
| Class selection | Added to registration | Required for username generation & enrollment |
| Select student by name | Verify with admission# + DOB | Privacy protection |
| Password = admission# | Allow but force change | Security + usability |
| Single email/phone | Supports multiple contacts | Real-world flexibility |

---

## 🚀 **This Design Supports:**

✅ Multi-tenancy (1000s of schools)  
✅ Horizontal scaling (microservices)  
✅ GDPR compliance (audit trail)  
✅ Mobile apps (JWT-based)  
✅ Event-driven (Azure Service Bus)  
✅ Real-time notifications (Twilio)  
✅ Role-based access control  
✅ Parent data protection (OTP)  

---

**Ready to implement?** Let's build the complete `IdentityDbContext` and all entity configurations! 🎯
