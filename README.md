# 🏫 Comprehensive School Management System (Microservices Architecture – .NET + Azure)

## Overview

This platform is a **comprehensive, cloud-based school management system** designed to unify all institutional operations — administration, academics, finance, communication, and parent engagement — under one modular architecture.

The system is built using:
- **Backend:** C# .NET (microservices architecture)
- **Database:** Azure SQL Database (normalized schema)
- **Messaging:** Azure Service Bus (event-driven)
- **Notifications:** Twilio (OTP, SMS, email)
- **Caching:** Azure Redis (OTP state, session cache)
- **Storage:** Azure Blob Storage (files, media)
- **Secrets & Keys:** Azure Key Vault

---

## 🎯 Core Goals

- Centralize all school operations (Admin, Academic, Finance, Parent)
- Provide real-time access to data and analytics
- Ensure privacy and confidentiality of student information
- Support multi-school (multi-tenant) deployments
- Enforce secure authentication and authorization across user roles
- Integrate OTP-based MFA (especially for parents)

---

## 🧱 System Architecture

The system is split into independent **microservices**, each owning its own data store and responsibilities:

| Service | Description | Technology |
|----------|--------------|-------------|
| **Auth Service** | Handles authentication, JWT issuance, password & OTP flows, MFA | ASP.NET Web API, Azure SQL, Redis |
| **User Directory Service** | Manages users, roles, contacts, memberships, and invitations | ASP.NET Web API |
| **School Service** | Registers schools, classes, and academic catalogs | ASP.NET Web API |
| **Student Service** | Manages student profiles, enrollments, and join tokens | ASP.NET Web API |
| **Guardian Service** | Links parents to students; enforces OTP-based access | ASP.NET Web API |
| **Notification Service** | Integrates with Twilio for SMS/Email OTP and notifications | .NET Background Service |
| **Finance Service** | Handles fee records, transactions, receipts (future) | ASP.NET Web API |
| **Academics Service** | Manages grades, assignments, and attendance (future) | ASP.NET Web API |
| **Gateway/API Aggregator** | Secures and routes traffic between clients and services | YARP / Azure API Management |

All inter-service communication uses **Azure Service Bus topics** (publish/subscribe pattern).

---

## 🔐 Authentication & Roles

### User Types
- **System Administrator:** Creates schools and principals.
- **Principal/Deputy Principal:** Manage school staff and oversee operations.
- **Teachers:** Manage classes, assignments, and student assessments.
- **Accountants:** Manage fee transactions and receipts.
- **Information Desk:** Handle parent communications and notices.
- **Transport Managers:** Manage routes and transport logs.
- **Students:** View learning materials, assignments, and progress.
- **Parents:** Access child’s academic and financial records (OTP-only).

### Authentication Policies
| Role | Auth Method | Account Creation |
|------|--------------|------------------|
| System Admin | Password + TOTP | Internal (super admin) |
| Principal/Deputy | Password + TOTP | Created by System Admin |
| Teachers/Staff | Password + TOTP | Invited by Principal/Deputy |
| Students | Password (initial = Admission No, must change) | Self-register via Enrollment |
| Parents | Password + OTP (or OTP-only) | Self-register via valid student link |

---

## ⚙️ Key Features

- Multi-tenant (supports multiple schools)
- OTP verification via Twilio for parents
- Role-based access control (RBAC)
- Secure JWT + refresh token authentication
- Staff invitations with one-time login links
- Parent–student linking with verification
- Class, enrollment, and fee management integration
- Outbox pattern for reliable event publishing to Azure Service Bus
- Full audit logging

---

## 🧩 Database Design (Azure SQL)

The system follows **3rd Normal Form (3NF)** — every piece of data is stored exactly once, ensuring consistency and performance.  
Below is a summary of the **key tables per schema** (from the normalized ERD).

---

### 🧠 `identity` schema — Authentication & Authorization

| Table | Purpose | Key Fields |
|--------|----------|------------|
| **Users** | Master record for every person in the system | `UserId`, `UserType`, `Status`, `CreatedAt` |
| **Contacts** | Stores verified emails/phones | `ContactId`, `UserId`, `Kind`, `Value`, `IsVerified` |
| **Credentials** | Passwords & MFA secrets | `UserId`, `PasswordHash`, `MfaMode`, `TotpSecret` |
| **Usernames** | School-scoped usernames (e.g., `ct201@meruschool`) | `UsernameId`, `UserId`, `SchoolId`, `Username` |
| **Roles** | Master roles (Principal, Teacher, etc.) | `RoleId`, `RoleCode`, `Description` |
| **Permissions** | Fine-grained permission codes | `PermissionId`, `PermCode` |
| **RolePermissions** | Maps roles to permissions | `RoleId`, `PermissionId` |
| **UserSchoolMemberships** | Which user belongs to which school | `MembershipId`, `UserId`, `SchoolId`, `RoleId` |
| **RefreshTokens** | Secure session management | `TokenId`, `UserId`, `IssuedAt`, `RevokedAt` |
| **LoginAttempts** | Tracks logins & failed attempts | `AttemptId`, `UserId`, `WhenAt`, `Result` |
| **AuditLog** | Tracks system actions | `AuditId`, `ActorUserId`, `Action`, `WhenAt` |

---

### 🏫 `school` schema — Tenant / School Catalog

| Table | Purpose | Key Fields |
|--------|----------|------------|
| **Schools** | Registered schools | `SchoolId`, `Slug`, `OfficialName`, `Status` |
| **Classes** | School classes/streams | `ClassId`, `SchoolId`, `Level`, `Stream`, `Year` |

---

### 🎓 `student` schema — Student Enrollment

| Table | Purpose | Key Fields |
|--------|----------|------------|
| **Students** | Student profiles (linked to Users) | `StudentId`, `UserId`, `OfficialNumber`, `DoB` |
| **StudentIdentifiers** | Admissions or UPI numbers | `Id`, `StudentId`, `SchoolId`, `Kind`, `Value` |
| **Enrollments** | Student membership in school/class | `EnrollmentId`, `StudentId`, `SchoolId`, `ClassId`, `Year`, `Status` |
| **JoinTokens** | Self-registration control tokens | `TokenId`, `EnrollmentId`, `ShortCode`, `ExpiresAt` |

---

### 👨‍👩‍👧 `guardian` schema — Parent/Guardian Links

| Table | Purpose | Key Fields |
|--------|----------|------------|
| **GuardianLinks** | Links parent users to student profiles | `GuardianLinkId`, `ParentUserId`, `StudentId`, `SchoolId`, `Relationship`, `VerifiedAt` |

---

### ✉️ `invite` schema — Staff & Parent Invitations

| Table | Purpose | Key Fields |
|--------|----------|------------|
| **Invitations** | Stores invites for new staff or parents | `InviteId`, `SchoolId`, `RoleId`, `TargetContact`, `FirstLoginUrlToken`, `ExpiresAt` |

---

### 🔔 `notify` schema — OTP / Notification History

| Table | Purpose | Key Fields |
|--------|----------|------------|
| **OtpRequests** | Tracks OTP send/verify history | `OtpId`, `UserId`, `Purpose`, `ContactKind`, `ContactValue`, `RequestedAt`, `Verified` |
| **MessageTemplates** | Stores reusable message templates | `TemplateId`, `Channel`, `Code`, `Subject`, `Body` |

---

### 🔄 `outbox` schema — Event Publishing

| Table | Purpose | Key Fields |
|--------|----------|------------|
| **IntegrationEvents** | Reliable event publishing to Azure Service Bus | `EventId`, `Topic`, `PayloadJson`, `PublishedAt` |

---

## 🧮 Normalization Summary

- **3NF** normalization ensures no redundant data.
- Each entity has its **own table** and uses **foreign keys** for relationships.
- **Lookup tables** (`Roles`, `Permissions`) are referenced by `UserSchoolMemberships`.
- **Soft deletion** and **status fields** used instead of hard deletes for audit consistency.
- High-read tables (e.g., `Enrollments`, `Memberships`, `Users`) have **covering indexes** on `SchoolId`, `Status`.

---

## 🧩 Entity Relationships (Simplified)

- `Users` ↔ `Usernames` (1-many)
- `Users` ↔ `Contacts` (1-many)
- `Users` ↔ `UserSchoolMemberships` ↔ `Schools` (many-many)
- `Users(Student)` ↔ `Students` ↔ `Enrollments` ↔ `Classes`
- `Users(Parent)` ↔ `GuardianLinks` ↔ `Students`
- `Users(Staff)` ↔ `Invitations` (1-many)
- `Schools` ↔ `Classes` (1-many)
- `Enrollments` ↔ `JoinTokens` (1-many)

---

## 🔐 Authentication Flow Summary

1. **System Admin** creates schools and principals.
2. **Principal/Deputy** invites teachers, accountants, etc.
3. **Students** self-register using admission/UPI + DoB or join token.
4. **Parents** self-register using verified phone/email and child’s info.
5. **Parents** login with **password + OTP** or **OTP-only** for maximum security.

---

## 🧰 Deployment Stack

| Layer | Tool / Service |
|--------|----------------|
| Hosting | Azure App Service / Azure Container Apps |
| Messaging | Azure Service Bus |
| Database | Azure SQL Database |
| Caching | Azure Redis Cache |
| Secrets | Azure Key Vault |
| File Storage | Azure Blob Storage |
| Monitoring | Azure Application Insights |
| Notifications | Twilio Verify API (SMS/Email) |

---

## 🛡️ Security Highlights

- **JWT tokens** with short access lifetimes & rotating refresh tokens.
- **MFA policies** (parents: OTP-only; staff: TOTP optional).
- **Role-based access** enforced by gateway and service policies.
- **Encrypted secrets** in Azure Key Vault.
- **Audit logging** for every login, invite, and data modification.
- **Least privilege principle** for all service accounts.

---

## 📄 License

This system is developed as part of an educational and administrative automation project.  
© 2025 — All Rights Reserved.

---

## 📧 Contact / Maintainers

**Lead Developer:** [Your Name]  
**Email:** [your.email@example.com]  
**Organization:** [School / Company Name]  
**Stack:** .NET 9, Azure SQL, Azure Service Bus, Twilio, Redis

