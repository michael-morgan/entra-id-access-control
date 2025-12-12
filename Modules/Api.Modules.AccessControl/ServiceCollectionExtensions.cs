using System.IdentityModel.Tokens.Jwt;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Configuration;
using Api.Modules.AccessControl.Authorization;
using Api.Modules.AccessControl.BusinessEvents;
using Api.Modules.AccessControl.Correlation;
using Api.Modules.AccessControl.Persistence;
using Casbin;
using Casbin.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Api.Modules.AccessControl;

/// <summary>
/// Extension methods for registering all AccessControl framework services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all AccessControl framework services.
    /// </summary>
    public static IServiceCollection AddAccessControlFramework(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ═══════════════════════════════════════════════════════════════════════
        // DATABASE CONTEXT (Unified - contains auth, events, audit schemas)
        // ═══════════════════════════════════════════════════════════════════════
        services.AddDbContext<AccessControlDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("AccessControlDb")));

        // ═══════════════════════════════════════════════════════════════════════
        // CORRELATION
        // ═══════════════════════════════════════════════════════════════════════
        services.AddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();
        services.AddScoped<CorrelationMiddleware>();
        services.AddScoped<IBusinessProcessManager, BusinessProcessManager>();
        services.AddScoped<IBackgroundCorrelationProvider, BackgroundCorrelationProvider>();
        services.Configure<Correlation.CorrelationOptions>(
            configuration.GetSection("AccessControl:Correlation"));

        // ═══════════════════════════════════════════════════════════════════════
        // REPOSITORIES
        // ═══════════════════════════════════════════════════════════════════════
        services.AddScoped<Persistence.Repositories.Authorization.IPolicyRepository, Persistence.Repositories.Authorization.PolicyRepository>();
        services.AddScoped<Persistence.Repositories.Authorization.IRoleRepository, Persistence.Repositories.Authorization.RoleRepository>();
        services.AddScoped<Persistence.Repositories.Authorization.IResourceRepository, Persistence.Repositories.Authorization.ResourceRepository>();
        services.AddScoped<Persistence.Repositories.Authorization.IUserRepository, Persistence.Repositories.Authorization.UserRepository>();
        services.AddScoped<Persistence.Repositories.Authorization.IGroupRepository, Persistence.Repositories.Authorization.GroupRepository>();
        services.AddScoped<Persistence.Repositories.Authorization.IUserGroupRepository, Persistence.Repositories.Authorization.UserGroupRepository>();
        services.AddScoped<Persistence.Repositories.Attributes.IGroupAttributeRepository, Persistence.Repositories.Attributes.GroupAttributeRepository>();
        services.AddScoped<Persistence.Repositories.Attributes.IUserAttributeRepository, Persistence.Repositories.Attributes.UserAttributeRepository>();
        services.AddScoped<Persistence.Repositories.AbacRules.IAbacRuleGroupRepository, Persistence.Repositories.AbacRules.AbacRuleGroupRepository>();
        services.AddScoped<Persistence.Repositories.AbacRules.IAbacRuleRepository, Persistence.Repositories.AbacRules.AbacRuleRepository>();
        services.AddScoped<Persistence.Repositories.Authorization.IAttributeSchemaRepository, Persistence.Repositories.Authorization.AttributeSchemaRepository>();
        services.AddScoped<Persistence.Repositories.Attributes.IRoleAttributeRepository, Persistence.Repositories.Attributes.RoleAttributeRepository>();

        // ═══════════════════════════════════════════════════════════════════════
        // JWT GROUP SYNC SERVICE
        // ═══════════════════════════════════════════════════════════════════════
        services.Configure<Configuration.GroupSyncOptions>(
            configuration.GetSection(Configuration.GroupSyncOptions.SectionName));
        services.AddScoped<Services.IJwtGroupSyncService, Services.JwtGroupSyncService>();

        // ═══════════════════════════════════════════════════════════════════════
        // AUTHORIZATION
        // ═══════════════════════════════════════════════════════════════════════

        // Casbin enforcer with SQL Server adapter
        services.AddScoped<SqlServerCasbinAdapter>();
        services.AddScoped<IEnforcer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<IEnforcer>>();
            logger.LogDebug("Creating new Casbin enforcer instance");

            var casbinModelPath = configuration["AccessControl:Authorization:CasbinModelPath"]
                ?? "casbin-model.conf";

            var model = DefaultModel.CreateFromFile(casbinModelPath);
            logger.LogDebug("Casbin model loaded from {ModelPath}", casbinModelPath);

            // Create enforcer with model
            var enforcer = new Enforcer(model);
            logger.LogDebug("Casbin enforcer instance created");

            // Load policies directly into enforcer from database
            var adapter = sp.GetRequiredService<SqlServerCasbinAdapter>();
            logger.LogDebug("Loading policies from database via adapter");

            // Load policies into the enforcer's internal policy store
            adapter.LoadPolicy(enforcer);
            logger.LogDebug("Policies loaded into enforcer");

            // Register ABAC functions
            enforcer.AddFunction("evalContext", (Func<string, string, string, string, string, bool>)CasbinAbacFunctions.EvalContext);
            enforcer.AddFunction("evalAbacRules", (Func<string, string, string, string, bool>)CasbinAbacFunctions.EvalAbacRules);
            logger.LogDebug("ABAC functions registered with Casbin enforcer");

            logger.LogInformation("Casbin enforcer initialized successfully");
            return enforcer;
        });

        // Policy engine abstraction (decouples from Casbin)
        services.AddScoped<IPolicyEngine, CasbinPolicyEngine>();

        // ABAC evaluation service (replaces Service Locator anti-pattern)
        services.AddScoped<IAbacEvaluationService, AbacEvaluationService>();

        // ABAC context building services (SRP-compliant specialized services)
        services.AddScoped<IAttributeMerger, AttributeMerger>();
        services.AddScoped<IResourceAttributeExtractor, ResourceAttributeExtractor>();
        services.AddScoped<IEnvironmentContextProvider, EnvironmentContextProvider>();
        services.AddScoped<IAbacContextProvider, DefaultAbacContextProvider>();

        services.AddScoped<IAuthorizationEnforcer, AuthorizationEnforcer>();
        services.AddScoped<IAuthorizationHandler, CasbinAuthorizationHandler>();
        services.AddScoped<IUserAttributeStore, SqlUserAttributeStore>();
        services.AddScoped<IGroupAttributeStore, SqlGroupAttributeStore>();
        services.AddScoped<IRoleAttributeStore, SqlRoleAttributeStore>();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddSingleton<IAuthorizationPolicyProvider, ResourcePolicyProvider>();

        // ABAC evaluator registry
        // Note: Each workstream must register its evaluators as IWorkstreamAbacEvaluator
        // Example: services.AddSingleton<IWorkstreamAbacEvaluator, LoansAbacEvaluator>();
        services.AddSingleton<IWorkstreamAbacEvaluatorRegistry, WorkstreamAbacEvaluatorRegistry>();

        services.Configure<Authorization.AuthorizationOptions>(
            configuration.GetSection("AccessControl:Authorization"));

        services.AddMemoryCache();

        // ═══════════════════════════════════════════════════════════════════════
        // BUSINESS EVENTS
        // ═══════════════════════════════════════════════════════════════════════
        services.AddScoped<IBusinessEventPublisher, BusinessEventPublisher>();
        services.AddScoped<IBusinessEventStore, BusinessEventStore>();
        services.AddScoped<IBusinessEventQueryService, BusinessEventQueryService>();

        // ═══════════════════════════════════════════════════════════════════════
        // AUDIT
        // ═══════════════════════════════════════════════════════════════════════
        services.AddEnterpriseAudit(configuration);

        // ═══════════════════════════════════════════════════════════════════════
        // HTTP CONTEXT
        // ═══════════════════════════════════════════════════════════════════════
        services.AddHttpContextAccessor();

        // ═══════════════════════════════════════════════════════════════════════
        // API CONTROLLERS
        // ═══════════════════════════════════════════════════════════════════════
        // Register controllers from this module for external integration
        services.AddControllers()
            .AddApplicationPart(typeof(ServiceCollectionExtensions).Assembly);

        return services;
    }

    /// <summary>
    /// Initializes ABAC functions with the application's service provider.
    /// Call this during application startup after services are built.
    /// </summary>
    public static IApplicationBuilder UseAccessControlAbac(this IApplicationBuilder app)
    {
        // Initialize ABAC functions with service provider for DI access
        CasbinAbacFunctions.Initialize(app.ApplicationServices);
        return app;
    }

    /// <summary>
    /// Configures Entra ID (Azure AD) authentication with JWT Bearer tokens.
    /// </summary>
    public static IServiceCollection AddEntraIdAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // CRITICAL: Clear the default inbound claim type map to preserve original JWT claims
        // By default, JwtSecurityTokenHandler remaps claim types (e.g., "sub" -> ClaimTypes.NameIdentifier)
        // This causes the "oid" claim to be lost during token validation
        // We need the raw "oid" claim for Entra ID user identification per Microsoft best practices
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var authority = configuration["EntraId:Authority"];
                var tenantId = configuration["EntraId:TenantId"];

                if (string.IsNullOrWhiteSpace(authority))
                    throw new InvalidOperationException("EntraId:Authority is required");

                if (string.IsNullOrWhiteSpace(tenantId))
                    throw new InvalidOperationException("EntraId:TenantId is required");

                // Note: ClientId is NOT required for JWT Bearer authentication (API-only scenario)
                // It's only needed if you enable Graph API feature (for Client Credentials flow)
                // For JWT validation, we only validate the Tenant ID (issuer) - Audience is disabled

                options.Authority = authority.Replace("{tenantId}", tenantId);
                options.RequireHttpsMetadata = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false, // Audience validation disabled - only validate Tenant ID
                    ValidateLifetime = true, // Enabled for security - validates token expiration
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew for distributed systems
                    // Accept both v1.0 and v2.0 tokens
                    ValidIssuers = new[]
                    {
                        $"https://login.microsoftonline.com/{tenantId}/v2.0",
                        $"https://sts.windows.net/{tenantId}/"
                    },
                    // CRITICAL: Disable default claim type mapping to preserve original JWT claims
                    // By default, ASP.NET Core transforms claim types (e.g., "oid" gets filtered out)
                    // We need the raw "oid" claim for Entra ID user identification
                    NameClaimType = "name",  // Keep "name" as display name
                    RoleClaimType = "roles"  // Keep "roles" for role claims
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning("JWT authentication failed: {Message}", context.Exception.Message);
                        logger.LogDebug(context.Exception, "JWT authentication failure details");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        var subject = context.Principal?.FindFirst("sub")?.Value ?? context.Principal?.FindFirst("oid")?.Value;
                        logger.LogInformation("JWT token validated successfully for subject: {Subject}", subject);
                        logger.LogDebug("Token validation complete");
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogDebug("JWT token received, beginning validation");
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    /// <summary>
    /// Configures ASP.NET Core authorization with our custom policies.
    /// </summary>
    public static IServiceCollection AddAccessControlAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddFallbackPolicy("AllAuthenticated", policy =>
            {
                policy.RequireAuthenticatedUser();
            });

        return services;
    }
}

/// <summary>
/// Extension methods for configuring the middleware pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Uses the AccessControl framework middleware.
    /// Call this AFTER UseAuthentication() and BEFORE UseAuthorization().
    /// </summary>
    public static IApplicationBuilder UseAccessControlFramework(
        this IApplicationBuilder app)
    {
        // Correlation middleware enriches logging and provides context
        app.UseMiddleware<CorrelationMiddleware>();

        return app;
    }

    /// <summary>
    /// Uses the JWT group synchronization middleware.
    /// Extracts group memberships from JWT tokens and persists to database for UI display.
    /// Call this AFTER UseAuthentication() and BEFORE UseAuthorization().
    /// </summary>
    /// <remarks>
    /// This middleware is optional. If not used, groups won't be available in the admin UI,
    /// but authorization will still work (JWT is the source of truth for authorization).
    ///
    /// Usage in API modules:
    /// app.UseAuthentication();
    /// app.UseGroupSync(); // <-- Add here
    /// app.UseAccessControlFramework();
    /// app.UseAuthorization();
    /// </remarks>
    public static IApplicationBuilder UseGroupSync(this IApplicationBuilder app)
    {
        app.UseMiddleware<Middleware.GroupSyncMiddleware>();
        return app;
    }
}
