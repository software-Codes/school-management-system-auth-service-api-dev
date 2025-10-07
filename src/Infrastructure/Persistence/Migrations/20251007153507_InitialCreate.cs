using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "guardian");

            migrationBuilder.EnsureSchema(
                name: "school");

            migrationBuilder.EnsureSchema(
                name: "student");

            migrationBuilder.CreateTable(
                name: "Contacts",
                schema: "identity",
                columns: table => new
                {
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.ContactId);
                });

            migrationBuilder.CreateTable(
                name: "Credentials",
                schema: "identity",
                columns: table => new
                {
                    CredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    MfaMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotpSecret = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    LastPasswordChangedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credentials", x => x.CredentialId);
                });

            migrationBuilder.CreateTable(
                name: "GuardianLinks",
                schema: "guardian",
                columns: table => new
                {
                    GuardianLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuardianLinks", x => x.GuardianLinkId);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "identity",
                columns: table => new
                {
                    TokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RevokedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.TokenId);
                });

            migrationBuilder.CreateTable(
                name: "Schools",
                schema: "school",
                columns: table => new
                {
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OfficialName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmisCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.SchoolId);
                });

            migrationBuilder.CreateTable(
                name: "StudentIdentifiers",
                schema: "student",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentIdentifiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                schema: "student",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficialNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DoB = table.Column<DateTime>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.StudentId);
                });

            migrationBuilder.CreateTable(
                name: "Usernames",
                schema: "identity",
                columns: table => new
                {
                    UsernameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usernames", x => x.UsernameId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "UserSchoolMemberships",
                schema: "identity",
                columns: table => new
                {
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSchoolMemberships", x => x.MembershipId);
                    table.ForeignKey(
                        name: "FK_UserSchoolMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_UserId",
                schema: "identity",
                table: "Contacts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_UserId_Kind_Value",
                schema: "identity",
                table: "Contacts",
                columns: new[] { "UserId", "Kind", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_Value_Kind_IsVerified",
                schema: "identity",
                table: "Contacts",
                columns: new[] { "Value", "Kind", "IsVerified" });

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_UserId",
                schema: "identity",
                table: "Credentials",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuardianLinks_ParentUserId_Status",
                schema: "guardian",
                table: "GuardianLinks",
                columns: new[] { "ParentUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GuardianLinks_ParentUserId_StudentId",
                schema: "guardian",
                table: "GuardianLinks",
                columns: new[] { "ParentUserId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuardianLinks_SchoolId_Status",
                schema: "guardian",
                table: "GuardianLinks",
                columns: new[] { "SchoolId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GuardianLinks_StudentId_Status",
                schema: "guardian",
                table: "GuardianLinks",
                columns: new[] { "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                schema: "identity",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ExpiresAt_RevokedAt",
                schema: "identity",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresAt", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Schools_EmisCode",
                schema: "school",
                table: "Schools",
                column: "EmisCode",
                unique: true,
                filter: "[EmisCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Schools_Slug",
                schema: "school",
                table: "Schools",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schools_Status",
                schema: "school",
                table: "Schools",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StudentIdentifiers_SchoolId_Kind_Value",
                schema: "student",
                table: "StudentIdentifiers",
                columns: new[] { "SchoolId", "Kind", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentIdentifiers_StudentId",
                schema: "student",
                table: "StudentIdentifiers",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_UserId",
                schema: "student",
                table: "Students",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usernames_SchoolId_Username",
                schema: "identity",
                table: "Usernames",
                columns: new[] { "SchoolId", "Username" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usernames_Username",
                schema: "identity",
                table: "Usernames",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                schema: "identity",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserType_Status",
                schema: "identity",
                table: "Users",
                columns: new[] { "UserType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSchoolMemberships_SchoolId_RoleId_Status",
                schema: "identity",
                table: "UserSchoolMemberships",
                columns: new[] { "SchoolId", "RoleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSchoolMemberships_UserId_SchoolId_RoleId",
                schema: "identity",
                table: "UserSchoolMemberships",
                columns: new[] { "UserId", "SchoolId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSchoolMemberships_UserId_SchoolId_Status",
                schema: "identity",
                table: "UserSchoolMemberships",
                columns: new[] { "UserId", "SchoolId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contacts",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Credentials",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "GuardianLinks",
                schema: "guardian");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Schools",
                schema: "school");

            migrationBuilder.DropTable(
                name: "StudentIdentifiers",
                schema: "student");

            migrationBuilder.DropTable(
                name: "Students",
                schema: "student");

            migrationBuilder.DropTable(
                name: "Usernames",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserSchoolMemberships",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "identity");
        }
    }
}
