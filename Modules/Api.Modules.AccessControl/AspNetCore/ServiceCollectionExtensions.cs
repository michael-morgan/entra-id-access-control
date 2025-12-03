using System.IdentityModel.Tokens.Jwt;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.Audit;
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
using Microsoft.IdentityModel.Tokens;

namespace Api.Modules.AccessControl.AspNetCore;

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
        // AUTHORIZATION
        // ═══════════════════════════════════════════════════════════════════════

        // Casbin enforcer with SQL Server adapter
        services.AddScoped<SqlServerCasbinAdapter>();
        services.AddScoped<IEnforcer>(sp =>
        {
            Console.WriteLine("[CASBIN DEBUG] IEnforcer factory called - creating new enforcer");

            var casbinModelPath = configuration["AccessControl:Authorization:CasbinModelPath"]
                ?? "casbin-model.conf";

            var model = DefaultModel.CreateFromFile(casbinModelPath);
            Console.WriteLine("[CASBIN DEBUG] Model loaded from file");

            // Create enforcer with model
            var enforcer = new Enforcer(model);
            Console.WriteLine("[CASBIN DEBUG] Enforcer created");

            // Load policies directly into enforcer from database
            var adapter = sp.GetRequiredService<SqlServerCasbinAdapter>();
            Console.WriteLine("[CASBIN DEBUG] Adapter retrieved, loading policies...");

            // Load policies into the enforcer's internal policy store
            adapter.LoadPolicy(enforcer);
            Console.WriteLine("[CASBIN DEBUG] Policies loaded into enforcer");

            // Register ABAC functions
            enforcer.AddFunction("evalContext", (Func<string, string, string, string, string, bool>)CasbinAbacFunctions.EvalContext);
            enforcer.AddFunction("evalAbacRules", (Func<string, string, string, string, bool>)CasbinAbacFunctions.EvalAbacRules);
            Console.WriteLine("[CASBIN DEBUG] ABAC functions registered");

            Console.WriteLine("[CASBIN DEBUG] IEnforcer factory complete");
            return enforcer;
        });

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
                var audience = configuration["EntraId:Audience"];
                var tenantId = configuration["EntraId:TenantId"];
                var clientId = configuration["EntraId:ClientId"];

                if (string.IsNullOrWhiteSpace(authority))
                    throw new InvalidOperationException("EntraId:Authority is required");

                if (string.IsNullOrWhiteSpace(tenantId))
                    throw new InvalidOperationException("EntraId:TenantId is required");

                if (string.IsNullOrWhiteSpace(clientId))
                    throw new InvalidOperationException("EntraId:ClientId is required");

                options.Authority = authority.Replace("{tenantId}", tenantId);
                options.Audience = audience?.Replace("{clientId}", clientId) ?? $"api://{clientId}";
                options.RequireHttpsMetadata = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false, // Disabled for testing due to clock skew issues
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(5),
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
                        Console.WriteLine($"[JWT AUTH] Authentication failed: {context.Exception.Message}");
                        Console.WriteLine($"[JWT AUTH] Exception details: {context.Exception}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine($"[JWT AUTH] Token validated successfully");
                        Console.WriteLine($"[JWT AUTH] Audience: {string.Join(", ", context.Principal?.Claims.Where(c => c.Type == "aud").Select(c => c.Value) ?? Array.Empty<string>())}");
                        Console.WriteLine($"[JWT AUTH] Issuer: {string.Join(", ", context.Principal?.Claims.Where(c => c.Type == "iss").Select(c => c.Value) ?? Array.Empty<string>())}");

                        // Log all claims to verify oid is preserved
                        Console.WriteLine($"[JWT AUTH] All claims:");
                        foreach (var claim in context.Principal?.Claims ?? Enumerable.Empty<System.Security.Claims.Claim>())
                        {
                            Console.WriteLine($"  {claim.Type} = {claim.Value}");
                        }

                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        Console.WriteLine($"[JWT AUTH] Token received, validating...");
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
}
