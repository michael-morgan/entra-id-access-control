using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Modules.AccessControl.Persistence.Migrations.Authorization
{
    /// <inheritdoc />
    public partial class AddAttributeSchemasAndAbacRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AbacRules",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkstreamId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RuleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Configuration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    FailureMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbacRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttributeSchemas",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkstreamId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AttributeLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AttributeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AttributeDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidationRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeSchemas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbacRules_Priority",
                schema: "auth",
                table: "AbacRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_AbacRules_Workstream_Active",
                schema: "auth",
                table: "AbacRules",
                columns: new[] { "WorkstreamId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeSchemas_Workstream",
                schema: "auth",
                table: "AttributeSchemas",
                column: "WorkstreamId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeSchemas_Workstream_Level_Name",
                schema: "auth",
                table: "AttributeSchemas",
                columns: new[] { "WorkstreamId", "AttributeLevel", "AttributeName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbacRules",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "AttributeSchemas",
                schema: "auth");
        }
    }
}
