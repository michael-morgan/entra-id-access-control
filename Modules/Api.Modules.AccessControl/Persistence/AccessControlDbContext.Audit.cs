using Api.Modules.AccessControl.Persistence.Entities.Audit;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence;

/// <summary>
/// Audit schema (audit) - partial class for AccessControlDbContext.
/// </summary>
public partial class AccessControlDbContext
{
    // Audit DbSets (audit schema)
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    partial void OnModelCreatingAudit(ModelBuilder modelBuilder)
    {
        // AuditLog indexes
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs", "audit");

            entity.HasIndex(e => e.UpdatedAt)
                .HasDatabaseName("IX_AuditLogs_UpdatedAt");

            entity.HasIndex(e => new { e.EntityType, e.EntityId })
                .HasDatabaseName("IX_AuditLogs_Entity");

            entity.HasIndex(e => new { e.UserId, e.UpdatedAt })
                .HasDatabaseName("IX_AuditLogs_User_UpdatedAt");

            entity.HasIndex(e => new { e.WorkstreamId, e.UpdatedAt })
                .HasDatabaseName("IX_AuditLogs_Workstream_UpdatedAt");

            entity.HasIndex(e => e.RequestCorrelationId)
                .HasDatabaseName("IX_AuditLogs_RequestCorrelation");

            entity.HasIndex(e => e.BusinessProcessId)
                .HasDatabaseName("IX_AuditLogs_BusinessProcess");
        });
    }
}
