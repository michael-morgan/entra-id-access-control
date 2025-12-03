using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Modules.AccessControl.Persistence.Migrations.Audit
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "audit",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkstreamId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestCorrelationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BusinessProcessId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AuditData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_BusinessProcess",
                schema: "audit",
                table: "AuditLogs",
                column: "BusinessProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entity",
                schema: "audit",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_RequestCorrelation",
                schema: "audit",
                table: "AuditLogs",
                column: "RequestCorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UpdatedAt",
                schema: "audit",
                table: "AuditLogs",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_User_UpdatedAt",
                schema: "audit",
                table: "AuditLogs",
                columns: new[] { "UserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Workstream_UpdatedAt",
                schema: "audit",
                table: "AuditLogs",
                columns: new[] { "WorkstreamId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "audit");
        }
    }
}
