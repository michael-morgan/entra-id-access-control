using Api.Modules.AccessControl.Persistence.Entities.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence;

/// <summary>
/// Authorization schema (auth) - partial class for AccessControlDbContext.
/// </summary>
public partial class AccessControlDbContext
{
    // Authorization DbSets (auth schema)
    public DbSet<CasbinPolicy> CasbinPolicies => Set<CasbinPolicy>();
    public DbSet<CasbinRole> CasbinRoles => Set<CasbinRole>();
    public DbSet<CasbinResource> CasbinResources => Set<CasbinResource>();
    public DbSet<UserAttribute> UserAttributes => Set<UserAttribute>();
    public DbSet<GroupAttribute> GroupAttributes => Set<GroupAttribute>();
    public DbSet<RoleAttribute> RoleAttributes => Set<RoleAttribute>();
    public DbSet<AttributeSchema> AttributeSchemas => Set<AttributeSchema>();
    public DbSet<AbacRule> AbacRules => Set<AbacRule>();
    public DbSet<AbacRuleGroup> AbacRuleGroups => Set<AbacRuleGroup>();

    partial void OnModelCreatingAuthorization(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("auth");

        // CasbinPolicy indexes
        modelBuilder.Entity<CasbinPolicy>(entity =>
        {
            entity.ToTable("CasbinPolicies", "auth");

            entity.HasIndex(e => new { e.PolicyType, e.V0, e.V1, e.V2, e.V3, e.V4 })
                .HasDatabaseName("IX_CasbinPolicies_Policy");

            entity.HasIndex(e => new { e.WorkstreamId, e.IsActive })
                .HasDatabaseName("IX_CasbinPolicies_Workstream");
        });

        // CasbinRole indexes
        modelBuilder.Entity<CasbinRole>(entity =>
        {
            entity.ToTable("CasbinRoles", "auth");

            entity.HasIndex(e => new { e.RoleName, e.WorkstreamId })
                .IsUnique()
                .HasDatabaseName("IX_CasbinRoles_RoleName_Workstream");

            entity.HasIndex(e => e.WorkstreamId)
                .HasDatabaseName("IX_CasbinRoles_Workstream");
        });

        // CasbinResource indexes
        modelBuilder.Entity<CasbinResource>(entity =>
        {
            entity.ToTable("CasbinResources", "auth");

            entity.HasIndex(e => new { e.ResourcePattern, e.WorkstreamId })
                .IsUnique()
                .HasDatabaseName("IX_CasbinResources_Pattern_Workstream");
        });

        // UserAttribute indexes - composite unique on UserId + WorkstreamId
        modelBuilder.Entity<UserAttribute>(entity =>
        {
            entity.ToTable("UserAttributes", "auth");

            entity.HasIndex(e => new { e.UserId, e.WorkstreamId })
                .IsUnique()
                .HasDatabaseName("IX_UserAttributes_UserId_Workstream");

            entity.HasIndex(e => e.WorkstreamId)
                .HasDatabaseName("IX_UserAttributes_Workstream");
        });

        // GroupAttribute indexes - composite unique on GroupId + WorkstreamId
        modelBuilder.Entity<GroupAttribute>(entity =>
        {
            entity.ToTable("GroupAttributes", "auth");

            entity.HasIndex(e => new { e.GroupId, e.WorkstreamId })
                .IsUnique()
                .HasDatabaseName("IX_GroupAttributes_GroupId_Workstream");

            entity.HasIndex(e => e.WorkstreamId)
                .HasDatabaseName("IX_GroupAttributes_Workstream");
        });

        // RoleAttribute indexes - composite unique on AppRoleId + WorkstreamId
        modelBuilder.Entity<RoleAttribute>(entity =>
        {
            entity.ToTable("RoleAttributes", "auth");

            entity.HasIndex(e => new { e.AppRoleId, e.WorkstreamId })
                .IsUnique()
                .HasDatabaseName("IX_RoleAttributes_AppRoleId_Workstream");

            entity.HasIndex(e => e.WorkstreamId)
                .HasDatabaseName("IX_RoleAttributes_Workstream");

            entity.HasIndex(e => new { e.RoleValue, e.WorkstreamId })
                .HasDatabaseName("IX_RoleAttributes_RoleValue_Workstream");
        });

        // AttributeSchema indexes - composite unique on WorkstreamId + AttributeLevel + AttributeName
        modelBuilder.Entity<AttributeSchema>(entity =>
        {
            entity.ToTable("AttributeSchemas", "auth");

            entity.HasIndex(e => new { e.WorkstreamId, e.AttributeLevel, e.AttributeName })
                .IsUnique()
                .HasDatabaseName("IX_AttributeSchemas_Workstream_Level_Name");

            entity.HasIndex(e => e.WorkstreamId)
                .HasDatabaseName("IX_AttributeSchemas_Workstream");
        });

        // AbacRule indexes
        modelBuilder.Entity<AbacRule>(entity =>
        {
            entity.ToTable("AbacRules", "auth");

            entity.HasIndex(e => new { e.WorkstreamId, e.IsActive })
                .HasDatabaseName("IX_AbacRules_Workstream_Active");

            entity.HasIndex(e => e.Priority)
                .HasDatabaseName("IX_AbacRules_Priority");

            entity.HasIndex(e => e.RuleGroupId)
                .HasDatabaseName("IX_AbacRules_RuleGroupId");
        });

        // AbacRuleGroup indexes
        modelBuilder.Entity<AbacRuleGroup>(entity =>
        {
            entity.ToTable("AbacRuleGroups", "auth");

            entity.HasIndex(e => new { e.WorkstreamId, e.IsActive })
                .HasDatabaseName("IX_AbacRuleGroups_Workstream_Active");

            entity.HasIndex(e => e.Priority)
                .HasDatabaseName("IX_AbacRuleGroups_Priority");

            entity.HasIndex(e => e.ParentGroupId)
                .HasDatabaseName("IX_AbacRuleGroups_ParentGroupId");

            entity.HasIndex(e => new { e.WorkstreamId, e.GroupName })
                .IsUnique()
                .HasDatabaseName("IX_AbacRuleGroups_Workstream_GroupName");
        });
    }
}
