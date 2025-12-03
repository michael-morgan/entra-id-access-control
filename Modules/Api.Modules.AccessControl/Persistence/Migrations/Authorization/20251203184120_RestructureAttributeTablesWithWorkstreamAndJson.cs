using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Modules.AccessControl.Persistence.Migrations.Authorization
{
    /// <inheritdoc />
    public partial class RestructureAttributeTablesWithWorkstreamAndJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserAttributes_UserId",
                schema: "auth",
                table: "UserAttributes");

            migrationBuilder.DropIndex(
                name: "IX_RoleAttributes_AppRoleId",
                schema: "auth",
                table: "RoleAttributes");

            migrationBuilder.DropIndex(
                name: "IX_RoleAttributes_RoleValue",
                schema: "auth",
                table: "RoleAttributes");

            migrationBuilder.DropIndex(
                name: "IX_GroupAttributes_GroupId",
                schema: "auth",
                table: "GroupAttributes");

            migrationBuilder.DropColumn(
                name: "ApprovalLimit",
                schema: "auth",
                table: "UserAttributes");

            migrationBuilder.DropColumn(
                name: "Department",
                schema: "auth",
                table: "UserAttributes");

            migrationBuilder.DropColumn(
                name: "HasGlobalAccess",
                schema: "auth",
                table: "UserAttributes");

            migrationBuilder.DropColumn(
                name: "ManagementLevel",
                schema: "auth",
                table: "UserAttributes");

            migrationBuilder.DropColumn(
                name: "Region",
                schema: "auth",
                table: "UserAttributes");

            migrationBuilder.DropColumn(
                name: "WorkstreamIds",
                schema: "auth",
                table: "UserAttributes");

            migrationBuilder.DropColumn(
                name: "ApprovalLimit",
                schema: "auth",
                table: "RoleAttributes");

            migrationBuilder.DropColumn(
                name: "DataSensitivityLevel",
                schema: "auth",
                table: "RoleAttributes");

            migrationBuilder.DropColumn(
                name: "ManagementLevel",
                schema: "auth",
                table: "RoleAttributes");

            migrationBuilder.DropColumn(
                name: "TransactionLimit",
                schema: "auth",
                table: "RoleAttributes");

            migrationBuilder.DropColumn(
                name: "ApprovalLimit",
                schema: "auth",
                table: "GroupAttributes");

            migrationBuilder.DropColumn(
                name: "BusinessUnit",
                schema: "auth",
                table: "GroupAttributes");

            migrationBuilder.DropColumn(
                name: "CostCenter",
                schema: "auth",
                table: "GroupAttributes");

            migrationBuilder.DropColumn(
                name: "Department",
                schema: "auth",
                table: "GroupAttributes");

            migrationBuilder.DropColumn(
                name: "ManagementLevel",
                schema: "auth",
                table: "GroupAttributes");

            migrationBuilder.DropColumn(
                name: "Region",
                schema: "auth",
                table: "GroupAttributes");

            migrationBuilder.RenameColumn(
                name: "CustomAttributesJson",
                schema: "auth",
                table: "RoleAttributes",
                newName: "AttributesJson");

            migrationBuilder.RenameColumn(
                name: "CustomAttributesJson",
                schema: "auth",
                table: "GroupAttributes",
                newName: "AttributesJson");

            migrationBuilder.AddColumn<string>(
                name: "AttributesJson",
                schema: "auth",
                table: "UserAttributes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkstreamId",
                schema: "auth",
                table: "UserAttributes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkstreamId",
                schema: "auth",
                table: "RoleAttributes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkstreamId",
                schema: "auth",
                table: "GroupAttributes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserAttributes_UserId_Workstream",
                schema: "auth",
                table: "UserAttributes",
                columns: new[] { "UserId", "WorkstreamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAttributes_Workstream",
                schema: "auth",
                table: "UserAttributes",
                column: "WorkstreamId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAttributes_AppRoleId_Workstream",
                schema: "auth",
                table: "RoleAttributes",
                columns: new[] { "AppRoleId", "WorkstreamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleAttributes_RoleValue_Workstream",
                schema: "auth",
                table: "RoleAttributes",
                columns: new[] { "RoleValue", "WorkstreamId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleAttributes_Workstream",
                schema: "auth",
                table: "RoleAttributes",
                column: "WorkstreamId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupAttributes_GroupId_Workstream",
                schema: "auth",
                table: "GroupAttributes",
                columns: new[] { "GroupId", "WorkstreamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupAttributes_Workstream",
                schema: "auth",
                table: "GroupAttributes",
                column: "WorkstreamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserAttributes_UserId_Workstream",
                schema: "auth",
                table: "UserAttributes");

            migrationBuilder.DropIndex(
                name: "IX_UserAttributes_Workstream",
                schema: "auth",
                table: "UserAttributes");

            migrationBuilder.DropIndex(
                name: "IX_RoleAttributes_AppRoleId_Workstream",
                schema: "auth",
                table: "RoleAttributes");

            migrationBuilder.DropIndex(
                name: "IX_RoleAttributes_RoleValue_Workstream",
                schema: "auth",
                table: "RoleAttributes");

            migrationBuilder.DropIndex(
                name: "IX_RoleAttributes_Workstream",
                schema: "auth",
                table: "RoleAttributes");

            migrationBuilder.DropIndex(
                name: "IX_GroupAttributes_GroupId_Workstream",
                schema: "auth",
                table: "GroupAttributes");

            migrationBuilder.DropIndex(
                name: "IX_GroupAttributes_Workstream",
                schema: "auth",
                table: "GroupAttributes");

            migrationBuilder.DropColumn(
                name: "AttributesJson",
                schema: "auth",
                table: "UserAttributes");

            migrationBuilder.DropColumn(
                name: "WorkstreamId",
                schema: "auth",
                table: "UserAttributes");

            migrationBuilder.DropColumn(
                name: "WorkstreamId",
                schema: "auth",
                table: "RoleAttributes");

            migrationBuilder.DropColumn(
                name: "WorkstreamId",
                schema: "auth",
                table: "GroupAttributes");

            migrationBuilder.RenameColumn(
                name: "AttributesJson",
                schema: "auth",
                table: "RoleAttributes",
                newName: "CustomAttributesJson");

            migrationBuilder.RenameColumn(
                name: "AttributesJson",
                schema: "auth",
                table: "GroupAttributes",
                newName: "CustomAttributesJson");

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovalLimit",
                schema: "auth",
                table: "UserAttributes",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                schema: "auth",
                table: "UserAttributes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasGlobalAccess",
                schema: "auth",
                table: "UserAttributes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ManagementLevel",
                schema: "auth",
                table: "UserAttributes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                schema: "auth",
                table: "UserAttributes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkstreamIds",
                schema: "auth",
                table: "UserAttributes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovalLimit",
                schema: "auth",
                table: "RoleAttributes",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataSensitivityLevel",
                schema: "auth",
                table: "RoleAttributes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagementLevel",
                schema: "auth",
                table: "RoleAttributes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TransactionLimit",
                schema: "auth",
                table: "RoleAttributes",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovalLimit",
                schema: "auth",
                table: "GroupAttributes",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessUnit",
                schema: "auth",
                table: "GroupAttributes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostCenter",
                schema: "auth",
                table: "GroupAttributes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                schema: "auth",
                table: "GroupAttributes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagementLevel",
                schema: "auth",
                table: "GroupAttributes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                schema: "auth",
                table: "GroupAttributes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAttributes_UserId",
                schema: "auth",
                table: "UserAttributes",
                column: "UserId",
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

            migrationBuilder.CreateIndex(
                name: "IX_GroupAttributes_GroupId",
                schema: "auth",
                table: "GroupAttributes",
                column: "GroupId",
                unique: true);
        }
    }
}
