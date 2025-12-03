using Api.Modules.AccessControl.Audit;
using Api.Modules.AccessControl.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.AccessControl.Persistence;

/// <summary>
/// Unified DbContext for Access Control module with three schemas: auth, events, audit.
/// </summary>
public partial class AccessControlDbContext : DbContext, IAuditableDbContext
{
    public AccessControlDbContext(DbContextOptions<AccessControlDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingAuthorization(modelBuilder);
        OnModelCreatingEvents(modelBuilder);
        OnModelCreatingAudit(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    // Partial method declarations for each schema
    partial void OnModelCreatingAuthorization(ModelBuilder modelBuilder);
    partial void OnModelCreatingEvents(ModelBuilder modelBuilder);
    partial void OnModelCreatingAudit(ModelBuilder modelBuilder);
}
