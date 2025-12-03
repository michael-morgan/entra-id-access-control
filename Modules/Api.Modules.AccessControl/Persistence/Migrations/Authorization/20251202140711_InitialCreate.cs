using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Modules.AccessControl.Persistence.Migrations.Authorization
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.CreateTable(
                name: "CasbinPolicies",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    V0 = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    V1 = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    V2 = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    V3 = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    V4 = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    V5 = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    WorkstreamId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasbinPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CasbinResources",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourcePattern = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    WorkstreamId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ParentResource = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasbinResources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CasbinRoles",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkstreamId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasbinRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAttributes",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovalLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ManagementLevel = table.Column<int>(type: "int", nullable: true),
                    WorkstreamIds = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HasGlobalAccess = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAttributes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CasbinPolicies_Policy",
                schema: "auth",
                table: "CasbinPolicies",
                columns: new[] { "PolicyType", "V0", "V1", "V2", "V3", "V4" });

            migrationBuilder.CreateIndex(
                name: "IX_CasbinPolicies_Workstream",
                schema: "auth",
                table: "CasbinPolicies",
                columns: new[] { "WorkstreamId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CasbinResources_Pattern_Workstream",
                schema: "auth",
                table: "CasbinResources",
                columns: new[] { "ResourcePattern", "WorkstreamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CasbinRoles_RoleName_Workstream",
                schema: "auth",
                table: "CasbinRoles",
                columns: new[] { "RoleName", "WorkstreamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CasbinRoles_Workstream",
                schema: "auth",
                table: "CasbinRoles",
                column: "WorkstreamId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAttributes_UserId",
                schema: "auth",
                table: "UserAttributes",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CasbinPolicies",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "CasbinResources",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "CasbinRoles",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "UserAttributes",
                schema: "auth");
        }
    }
}
