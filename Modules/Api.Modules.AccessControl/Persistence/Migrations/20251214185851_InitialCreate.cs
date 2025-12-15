using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Modules.AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "events");

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
                name: "GroupAttributes",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkstreamId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AttributesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "Groups",
                schema: "auth",
                columns: table => new
                {
                    GroupId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.GroupId);
                });

            migrationBuilder.CreateTable(
                name: "RoleAttributes",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkstreamId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AttributesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAttributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAttributes",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    WorkstreamId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AttributesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAttributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "auth",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "AbacRules",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkstreamId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RuleGroupId = table.Column<int>(type: "int", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_AbacRules_AbacRuleGroups_RuleGroupId",
                        column: x => x.RuleGroupId,
                        principalSchema: "auth",
                        principalTable: "AbacRuleGroups",
                        principalColumn: "Id");
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

            migrationBuilder.CreateTable(
                name: "UserGroups",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    GroupId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "auth",
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGroups_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_AbacRules_Priority",
                schema: "auth",
                table: "AbacRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_AbacRules_RuleGroupId",
                schema: "auth",
                table: "AbacRules",
                column: "RuleGroupId");

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

            migrationBuilder.CreateIndex(
                name: "IX_RoleAttributes_RoleId_Workstream",
                schema: "auth",
                table: "RoleAttributes",
                columns: new[] { "RoleId", "WorkstreamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleAttributes_Workstream",
                schema: "auth",
                table: "RoleAttributes",
                column: "WorkstreamId");

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
                name: "IX_UserGroups_GroupId",
                schema: "auth",
                table: "UserGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_LastSeenAt",
                schema: "auth",
                table: "UserGroups",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_UserId_GroupId",
                schema: "auth",
                table: "UserGroups",
                columns: new[] { "UserId", "GroupId" },
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

            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "BusinessEvents",
                schema: "events");

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
                name: "GroupAttributes",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "RoleAttributes",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "UserAttributes",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "UserGroups",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "AbacRuleGroups",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "BusinessProcesses",
                schema: "events");

            migrationBuilder.DropTable(
                name: "Groups",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "auth");
        }
    }
}
