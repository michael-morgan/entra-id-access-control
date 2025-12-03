using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Modules.AccessControl.Persistence.Migrations.Events
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "events");

            migrationBuilder.CreateTable(
                name: "BusinessProcesses",
                schema: "events",
                columns: table => new
                {
                    BusinessProcessId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkstreamId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InitiatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    InitiatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessProcesses", x => x.BusinessProcessId);
                });

            migrationBuilder.CreateTable(
                name: "BusinessEvents",
                schema: "events",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessProcessId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SessionCorrelationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestCorrelationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WorkstreamId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EventCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventVersion = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ActorType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActorDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ActorIpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    EventData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AffectedEntities = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEvents", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_BusinessEvents_BusinessProcesses_BusinessProcessId",
                        column: x => x.BusinessProcessId,
                        principalSchema: "events",
                        principalTable: "BusinessProcesses",
                        principalColumn: "BusinessProcessId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEvents_Actor",
                schema: "events",
                table: "BusinessEvents",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEvents_Process_Sequence",
                schema: "events",
                table: "BusinessEvents",
                columns: new[] { "BusinessProcessId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEvents_Sequence",
                schema: "events",
                table: "BusinessEvents",
                column: "SequenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEvents_Type_Occurred",
                schema: "events",
                table: "BusinessEvents",
                columns: new[] { "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEvents_Workstream_Category_Occurred",
                schema: "events",
                table: "BusinessEvents",
                columns: new[] { "WorkstreamId", "EventCategory", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessProcesses_Type_Initiated",
                schema: "events",
                table: "BusinessProcesses",
                columns: new[] { "ProcessType", "InitiatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessProcesses_Workstream_Status",
                schema: "events",
                table: "BusinessProcesses",
                columns: new[] { "WorkstreamId", "Status" });

            // Create trigger to prevent updates and deletes on BusinessEvents (immutability)
            migrationBuilder.Sql(@"
                CREATE TRIGGER [events].[TR_BusinessEvents_PreventModification]
                ON [events].[BusinessEvents]
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (SELECT 1 FROM deleted)
                    BEGIN
                        DECLARE @ErrorMessage NVARCHAR(1000) =
                            'Business events are immutable and cannot be modified or deleted. ' +
                            'EventId: ' + CAST((SELECT TOP 1 EventId FROM deleted) AS NVARCHAR(50));

                        THROW 50001, @ErrorMessage, 1;
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop trigger first
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [events].[TR_BusinessEvents_PreventModification]");

            migrationBuilder.DropTable(
                name: "BusinessEvents",
                schema: "events");

            migrationBuilder.DropTable(
                name: "BusinessProcesses",
                schema: "events");
        }
    }
}
