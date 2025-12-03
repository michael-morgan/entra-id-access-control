using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Modules.AccessControl.Persistence.Migrations.Authorization
{
    /// <inheritdoc />
    public partial class AddAbacRuleGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RuleGroupId",
                schema: "auth",
                table: "AbacRules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AbacRuleGroups",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkstreamId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ParentGroupId = table.Column<int>(type: "int", nullable: true),
                    LogicalOperator = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbacRuleGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbacRuleGroups_AbacRuleGroups_ParentGroupId",
                        column: x => x.ParentGroupId,
                        principalSchema: "auth",
                        principalTable: "AbacRuleGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbacRules_RuleGroupId",
                schema: "auth",
                table: "AbacRules",
                column: "RuleGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AbacRuleGroups_ParentGroupId",
                schema: "auth",
                table: "AbacRuleGroups",
                column: "ParentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AbacRuleGroups_Priority",
                schema: "auth",
                table: "AbacRuleGroups",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_AbacRuleGroups_Workstream_Active",
                schema: "auth",
                table: "AbacRuleGroups",
                columns: new[] { "WorkstreamId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AbacRuleGroups_Workstream_GroupName",
                schema: "auth",
                table: "AbacRuleGroups",
                columns: new[] { "WorkstreamId", "GroupName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AbacRules_AbacRuleGroups_RuleGroupId",
                schema: "auth",
                table: "AbacRules",
                column: "RuleGroupId",
                principalSchema: "auth",
                principalTable: "AbacRuleGroups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbacRules_AbacRuleGroups_RuleGroupId",
                schema: "auth",
                table: "AbacRules");

            migrationBuilder.DropTable(
                name: "AbacRuleGroups",
                schema: "auth");

            migrationBuilder.DropIndex(
                name: "IX_AbacRules_RuleGroupId",
                schema: "auth",
                table: "AbacRules");

            migrationBuilder.DropColumn(
                name: "RuleGroupId",
                schema: "auth",
                table: "AbacRules");
        }
    }
}
