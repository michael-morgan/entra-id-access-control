using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Api.Modules.AccessControl.Persistence;

/// <summary>
/// Design-time factory for AccessControlDbContext.
/// Used by EF Core tools for migrations.
/// </summary>
public class AccessControlDbContextFactory : IDesignTimeDbContextFactory<AccessControlDbContext>
{
    public AccessControlDbContext CreateDbContext(string[] args)
    {
        // Try environment variable first
        var connectionString = Environment.GetEnvironmentVariable("AccessControlDb");

        // If not in environment, try user secrets
        if (string.IsNullOrEmpty(connectionString))
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<AccessControlDbContextFactory>()
                .Build();

            connectionString = configuration.GetConnectionString("AccessControlDb");
        }

        // If still not found, throw helpful error
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "AccessControlDb connection string is required for EF Core design-time operations. " +
                "Configure it using one of these methods:\n" +
                "1. User Secrets: dotnet user-secrets set \"ConnectionStrings:AccessControlDb\" \"your-connection-string\" --project Modules/Api.Modules.AccessControl\n" +
                "2. Environment Variable: $env:AccessControlDb=\"your-connection-string\" (PowerShell) or export AccessControlDb=\"your-connection-string\" (bash)");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AccessControlDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AccessControlDbContext(optionsBuilder.Options);
    }
}
