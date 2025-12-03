using Api.Modules.AccessControl.Persistence.Entities.Events;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence;

/// <summary>
/// Events schema (events) - partial class for AccessControlDbContext.
/// </summary>
public partial class AccessControlDbContext
{
    // Events DbSets (events schema)
    public DbSet<BusinessProcessEntity> BusinessProcesses => Set<BusinessProcessEntity>();
    public DbSet<StoredBusinessEvent> BusinessEvents => Set<StoredBusinessEvent>();

    partial void OnModelCreatingEvents(ModelBuilder modelBuilder)
    {
        // BusinessProcess indexes
        modelBuilder.Entity<BusinessProcessEntity>(entity =>
        {
            entity.ToTable("BusinessProcesses", "events");

            entity.HasIndex(e => new { e.WorkstreamId, e.Status })
                .HasDatabaseName("IX_BusinessProcesses_Workstream_Status");

            entity.HasIndex(e => new { e.ProcessType, e.InitiatedAt })
                .HasDatabaseName("IX_BusinessProcesses_Type_Initiated");
        });

        // BusinessEvent indexes and configuration
        modelBuilder.Entity<StoredBusinessEvent>(entity =>
        {
            entity.ToTable("BusinessEvents", "events");

            entity.HasIndex(e => e.SequenceNumber)
                .IsUnique()
                .HasDatabaseName("IX_BusinessEvents_Sequence");

            entity.HasIndex(e => new { e.BusinessProcessId, e.SequenceNumber })
                .HasDatabaseName("IX_BusinessEvents_Process_Sequence");

            entity.HasIndex(e => new { e.WorkstreamId, e.EventCategory, e.OccurredAt })
                .HasDatabaseName("IX_BusinessEvents_Workstream_Category_Occurred");

            entity.HasIndex(e => new { e.EventType, e.OccurredAt })
                .HasDatabaseName("IX_BusinessEvents_Type_Occurred");

            entity.HasIndex(e => e.ActorId)
                .HasDatabaseName("IX_BusinessEvents_Actor");

            // Set default value for RecordedAt
            entity.Property(e => e.RecordedAt)
                .HasDefaultValueSql("SYSDATETIMEOFFSET()");

            // Configure relationship
            entity.HasOne(e => e.BusinessProcess)
                .WithMany()
                .HasForeignKey(e => e.BusinessProcessId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
