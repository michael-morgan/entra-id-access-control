using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Modules.AccessControl.Persistence.Migrations.Authorization
{
    /// <inheritdoc />
    public partial class AddGroupAndRoleAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupAttributes",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ManagementLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ApprovalLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CostCenter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BusinessUnit = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CustomAttributesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupAttributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleAttributes",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppRoleId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoleValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RoleDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ManagementLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ApprovalLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TransactionLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DataSensitivityLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CustomAttributesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAttributes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupAttributes_GroupId",
                schema: "auth",
                table: "GroupAttributes",
                column: "GroupId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleAttributes_AppRoleId",
                schema: "auth",
                table: "RoleAttributes",
                column: "AppRoleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleAttributes_RoleValue",
                schema: "auth",
                table: "RoleAttributes",
                column: "RoleValue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupAttributes",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "RoleAttributes",
                schema: "auth");
        }
    }
}
