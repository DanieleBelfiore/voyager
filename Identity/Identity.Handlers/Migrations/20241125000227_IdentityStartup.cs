using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Handlers.Migrations
{
  /// <inheritdoc />
  public partial class IdentityStartup : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "OpenIddictApplications",
          columns: table => new
          {
            Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
            ApplicationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
            ClientSecret = table.Column<string>(type: "nvarchar(max)", nullable: true),
            ClientType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            ConsentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
            DisplayNames = table.Column<string>(type: "nvarchar(max)", nullable: true),
            JsonWebKeySet = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Permissions = table.Column<string>(type: "nvarchar(max)", nullable: true),
            PostLogoutRedirectUris = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
            RedirectUris = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Requirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Settings = table.Column<string>(type: "nvarchar(max)", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_OpenIddictApplications", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "OpenIddictScopes",
          columns: table => new
          {
            Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
            ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Descriptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
            DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
            DisplayNames = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
            Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Resources = table.Column<string>(type: "nvarchar(max)", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_OpenIddictScopes", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "Roles",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
            NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
            ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_Roles", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "Users",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            FirstName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
            LastName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
            Ratings = table.Column<double>(type: "float", nullable: false),
            PhoneNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
            IsDriver = table.Column<bool>(type: "bit", nullable: false),
            Created = table.Column<DateTime>(type: "DateTime", nullable: false),
            Modified = table.Column<DateTime>(type: "DateTime", nullable: false),
            LastLogin = table.Column<DateTime>(type: "datetime2", nullable: false),
            UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
            NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
            Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
            NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
            EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
            PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
            SecurityStamp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
            ConcurrencyStamp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
            PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
            TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
            LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
            LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
            AccessFailedCount = table.Column<int>(type: "int", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_Users", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "OpenIddictAuthorizations",
          columns: table => new
          {
            Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
            ApplicationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
            ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            CreationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
            Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Scopes = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            Subject = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
            Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_OpenIddictAuthorizations", x => x.Id);
            table.ForeignKey(
                      name: "FK_OpenIddictAuthorizations_OpenIddictApplications_ApplicationId",
                      column: x => x.ApplicationId,
                      principalTable: "OpenIddictApplications",
                      principalColumn: "Id");
          });

      migrationBuilder.CreateTable(
          name: "RoleClaims",
          columns: table => new
          {
            Id = table.Column<int>(type: "int", nullable: false)
                  .Annotation("SqlServer:Identity", "1, 1"),
            RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
            ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_RoleClaims", x => x.Id);
            table.ForeignKey(
                      name: "FK_RoleClaims_Roles_RoleId",
                      column: x => x.RoleId,
                      principalTable: "Roles",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "UserClaims",
          columns: table => new
          {
            Id = table.Column<int>(type: "int", nullable: false)
                  .Annotation("SqlServer:Identity", "1, 1"),
            UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
            ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_UserClaims", x => x.Id);
            table.ForeignKey(
                      name: "FK_UserClaims_Users_UserId",
                      column: x => x.UserId,
                      principalTable: "Users",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "UserLogins",
          columns: table => new
          {
            LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
            ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
            ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
            UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
            table.ForeignKey(
                      name: "FK_UserLogins_Users_UserId",
                      column: x => x.UserId,
                      principalTable: "Users",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "UserRoles",
          columns: table => new
          {
            UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
            table.ForeignKey(
                      name: "FK_UserRoles_Roles_RoleId",
                      column: x => x.RoleId,
                      principalTable: "Roles",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_UserRoles_Users_UserId",
                      column: x => x.UserId,
                      principalTable: "Users",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "UserTokens",
          columns: table => new
          {
            UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
            Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
            Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
            table.ForeignKey(
                      name: "FK_UserTokens_Users_UserId",
                      column: x => x.UserId,
                      principalTable: "Users",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "OpenIddictTokens",
          columns: table => new
          {
            Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
            ApplicationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
            AuthorizationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
            ConcurrencyToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            CreationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
            ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
            Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
            RedemptionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
            ReferenceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
            Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
            Subject = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
            Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_OpenIddictTokens", x => x.Id);
            table.ForeignKey(
                      name: "FK_OpenIddictTokens_OpenIddictApplications_ApplicationId",
                      column: x => x.ApplicationId,
                      principalTable: "OpenIddictApplications",
                      principalColumn: "Id");
            table.ForeignKey(
                      name: "FK_OpenIddictTokens_OpenIddictAuthorizations_AuthorizationId",
                      column: x => x.AuthorizationId,
                      principalTable: "OpenIddictAuthorizations",
                      principalColumn: "Id");
          });

      migrationBuilder.CreateIndex(
          name: "IX_OpenIddictApplications_ClientId",
          table: "OpenIddictApplications",
          column: "ClientId",
          unique: true,
          filter: "[ClientId] IS NOT NULL");

      migrationBuilder.CreateIndex(
          name: "IX_OpenIddictAuthorizations_ApplicationId_Status_Subject_Type",
          table: "OpenIddictAuthorizations",
          columns: new[] { "ApplicationId", "Status", "Subject", "Type" });

      migrationBuilder.CreateIndex(
          name: "IX_OpenIddictScopes_Name",
          table: "OpenIddictScopes",
          column: "Name",
          unique: true,
          filter: "[Name] IS NOT NULL");

      migrationBuilder.CreateIndex(
          name: "IX_OpenIddictTokens_ApplicationId_Status_Subject_Type",
          table: "OpenIddictTokens",
          columns: new[] { "ApplicationId", "Status", "Subject", "Type" });

      migrationBuilder.CreateIndex(
          name: "IX_OpenIddictTokens_AuthorizationId",
          table: "OpenIddictTokens",
          column: "AuthorizationId");

      migrationBuilder.CreateIndex(
          name: "IX_OpenIddictTokens_ReferenceId",
          table: "OpenIddictTokens",
          column: "ReferenceId",
          unique: true,
          filter: "[ReferenceId] IS NOT NULL");

      migrationBuilder.CreateIndex(
          name: "IX_RoleClaims_RoleId",
          table: "RoleClaims",
          column: "RoleId");

      migrationBuilder.CreateIndex(
          name: "RoleNameIndex",
          table: "Roles",
          column: "NormalizedName",
          unique: true,
          filter: "[NormalizedName] IS NOT NULL");

      migrationBuilder.CreateIndex(
          name: "IX_UserClaims_UserId",
          table: "UserClaims",
          column: "UserId");

      migrationBuilder.CreateIndex(
          name: "IX_UserLogins_UserId",
          table: "UserLogins",
          column: "UserId");

      migrationBuilder.CreateIndex(
          name: "IX_UserRoles_RoleId",
          table: "UserRoles",
          column: "RoleId");

      migrationBuilder.CreateIndex(
          name: "EmailIndex",
          table: "Users",
          column: "NormalizedEmail");

      migrationBuilder.CreateIndex(
          name: "UserNameIndex",
          table: "Users",
          column: "NormalizedUserName",
          unique: true);

      migrationBuilder.Sql(@"INSERT INTO [identity].[dbo].[OpenIddictApplications] ([Id],[ClientId],[ClientType],[DisplayName],[Permissions]) VALUES (NEWID(), 'voyager_app', 'public', 'Voyager App', '[""ept:token"",""ept:logout"",""rst:id_token"",""rst:id_token token"",""rst:token"",""gt:password"",""scp:email"",""scp:profile"",""scp:roles""]')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "OpenIddictScopes");

      migrationBuilder.DropTable(
          name: "OpenIddictTokens");

      migrationBuilder.DropTable(
          name: "RoleClaims");

      migrationBuilder.DropTable(
          name: "UserClaims");

      migrationBuilder.DropTable(
          name: "UserLogins");

      migrationBuilder.DropTable(
          name: "UserRoles");

      migrationBuilder.DropTable(
          name: "UserTokens");

      migrationBuilder.DropTable(
          name: "OpenIddictAuthorizations");

      migrationBuilder.DropTable(
          name: "Roles");

      migrationBuilder.DropTable(
          name: "Users");

      migrationBuilder.DropTable(
          name: "OpenIddictApplications");
    }
  }
}