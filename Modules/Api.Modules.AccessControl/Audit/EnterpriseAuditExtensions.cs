using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Persistence;
using Audit.Core;
using Audit.EntityFramework;
using Audit.SqlServer.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AuditConfiguration = Audit.Core.Configuration;
using AuditEventEntityFramework = Audit.EntityFramework.AuditEventEntityFramework;

namespace Api.Modules.AccessControl.Audit;

/// <summary>
/// Audit.NET configuration for SQL Server.
/// </summary>
public static class EnterpriseAuditExtensions
{
    public static IServiceCollection AddEnterpriseAudit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Audit.NET to use SQL Server
        AuditConfiguration.Setup()
            .UseSqlServer(config => config
                .ConnectionString(configuration.GetConnectionString("AuditDb"))
                .Schema("audit")
                .TableName("AuditLogs")
                .IdColumnName("AuditId")
                .JsonColumnName("AuditData")
                .LastUpdatedColumnName("UpdatedAt")
                .CustomColumn("UserId", ev => GetUserId(ev))
                .CustomColumn("WorkstreamId", ev => GetWorkstreamId(ev))
                .CustomColumn("RequestCorrelationId", ev => GetRequestCorrelationId(ev))
                .CustomColumn("BusinessProcessId", ev => GetBusinessProcessId(ev))
                .CustomColumn("EntityType", ev => GetEntityType(ev))
                .CustomColumn("EntityId", ev => GetEntityId(ev))
                .CustomColumn("Action", ev => ev.EventType)
                .CustomColumn("IpAddress", ev => GetIpAddress(ev)));

        return services;
    }

    private static string? GetUserId(AuditEvent ev)
    {
        if (ev.Environment.CustomFields.TryGetValue("CurrentUser", out var user) && user is ICurrentUserAccessor currentUser)
        {
            return currentUser.User.Id;
        }
        return null;
    }

    private static string? GetWorkstreamId(AuditEvent ev)
    {
        if (ev.Environment.CustomFields.TryGetValue("CorrelationContext", out var ctx) && ctx is ICorrelationContextAccessor correlation)
        {
            return correlation.Context?.WorkstreamId;
        }
        return null;
    }

    private static string? GetRequestCorrelationId(AuditEvent ev)
    {
        if (ev.Environment.CustomFields.TryGetValue("CorrelationContext", out var ctx) && ctx is ICorrelationContextAccessor correlation)
        {
            return correlation.Context?.RequestCorrelationId;
        }
        return null;
    }

    private static string? GetBusinessProcessId(AuditEvent ev)
    {
        if (ev.Environment.CustomFields.TryGetValue("CorrelationContext", out var ctx) && ctx is ICorrelationContextAccessor correlation)
        {
            return correlation.Context?.BusinessProcessId;
        }
        return null;
    }

    private static string? GetEntityType(AuditEvent ev)
    {
        if (ev is AuditEventEntityFramework efEvent)
        {
            return efEvent.EntityFrameworkEvent?.Entries?.FirstOrDefault()?.EntityType?.Name;
        }
        return null;
    }

    private static string? GetEntityId(AuditEvent ev)
    {
        if (ev is AuditEventEntityFramework efEvent)
        {
            var entry = efEvent.EntityFrameworkEvent?.Entries?.FirstOrDefault();
            if (entry != null)
            {
                var pk = entry.PrimaryKey?.FirstOrDefault();
                return pk?.Value?.ToString();
            }
        }
        return null;
    }

    private static string? GetIpAddress(AuditEvent ev)
    {
        if (ev.Environment.CustomFields.TryGetValue("CurrentUser", out var user) && user is ICurrentUserAccessor currentUser)
        {
            return currentUser.User.IpAddress;
        }
        return null;
    }
}
