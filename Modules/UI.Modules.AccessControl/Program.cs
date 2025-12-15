using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.BusinessEvents;
using Api.Modules.AccessControl.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using UI.Modules.AccessControl.Middleware;
using UI.Modules.AccessControl.Services.Graph;
using UI.Modules.AccessControl.Services.Testing;
using UI.Modules.AccessControl.Services.Attributes;
using UI.Modules.AccessControl.Services.Authorization.Policies;
using UI.Modules.AccessControl.Services.Authorization.Roles;
using UI.Modules.AccessControl.Services.Authorization.Resources;
using UI.Modules.AccessControl.Services.Authorization.AbacRules;
using UI.Modules.AccessControl.Services.Authorization.Users;
using UI.Modules.AccessControl.Services.Audit;
using UI.Modules.AccessControl.Services.Groups;

var builder = WebApplication.CreateBuilder(args);

// Add Feature Management
builder.Services.AddFeatureManagement();

// Configure authentication with Entra ID and Graph API
var initialScopes = builder.Configuration["DownstreamApi:Scopes"]?.Split(' ');

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("EntraId"))
        .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
            .AddMicrosoftGraph(builder.Configuration.GetSection("DownstreamApi"))
            .AddSessionTokenCaches(); // Session-based token cache for POC/demo

// Configure cookie authentication to align with session lifetime
// When session token cache is empty (e.g., after restart), sign out user to force re-auth
builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    // When accessing a page, validate that we can still acquire tokens
    options.Events.OnValidatePrincipal = async context =>
    {
        // This runs on every request with an authenticated user
        // We'll let the controllers handle MsalUiRequiredException and redirect to sign-in
        await Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// Configure anti-forgery for CSRF protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "X-CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Add session support for workstream context
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add memory cache for Graph API caching
builder.Services.AddMemoryCache();

// Add unified database context
builder.Services.AddDbContext<AccessControlDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AccessControlDb")));

// Add business event query service
builder.Services.AddScoped<IBusinessEventQueryService, BusinessEventQueryService>();

// Add repositories
builder.Services.AddScoped<Api.Modules.AccessControl.Persistence.Repositories.Authorization.IPolicyRepository, Api.Modules.AccessControl.Persistence.Repositories.Authorization.PolicyRepository>();
builder.Services.AddScoped<Api.Modules.AccessControl.Persistence.Repositories.Authorization.IRoleRepository, Api.Modules.AccessControl.Persistence.Repositories.Authorization.RoleRepository>();
builder.Services.AddScoped<Api.Modules.AccessControl.Persistence.Repositories.Authorization.ICasbinRoleRepository, Api.Modules.AccessControl.Persistence.Repositories.Authorization.CasbinRoleRepository>();
builder.Services.AddScoped<Api.Modules.AccessControl.Persistence.Repositories.Authorization.IResourceRepository, Api.Modules.AccessControl.Persistence.Repositories.Authorization.ResourceRepository>();
builder.Services.AddScoped<Api.Modules.AccessControl.Persistence.Repositories.Authorization.IUserRepository, Api.Modules.AccessControl.Persistence.Repositories.Authorization.UserRepository>();
builder.Services.AddScoped<Api.Modules.AccessControl.Persistence.Repositories.Authorization.IGroupRepository, Api.Modules.AccessControl.Persistence.Repositories.Authorization.GroupRepository>();
builder.Services.AddScoped<Api.Modules.AccessControl.Persistence.Repositories.Authorization.IUserGroupRepository, Api.Modules.AccessControl.Persistence.Repositories.Authorization.UserGroupRepository>();
builder.Services.AddScoped<Api.Modules.AccessControl.Persistence.Repositories.Attributes.IGroupAttributeRepository, Api.Modules.AccessControl.Persistence.Repositories.Attributes.GroupAttributeRepository>();
builder.Services.AddScoped<Api.Modules.AccessControl.Persistence.Repositories.Attributes.IUserAttributeRepository, Api.Modules.AccessControl.Persistence.Repositories.Attributes.UserAttributeRepository>();
builder.Services.AddScoped<Api.Modules.AccessControl.Persistence.Repositories.AbacRules.IAbacRuleGroupRepository, Api.Modules.AccessControl.Persistence.Repositories.AbacRules.AbacRuleGroupRepository>();
builder.Services.AddScoped<Api.Modules.AccessControl.Persistence.Repositories.AbacRules.IAbacRuleRepository, Api.Modules.AccessControl.Persistence.Repositories.AbacRules.AbacRuleRepository>();
builder.Services.AddScoped<Api.Modules.AccessControl.Persistence.Repositories.Authorization.IAttributeSchemaRepository, Api.Modules.AccessControl.Persistence.Repositories.Authorization.AttributeSchemaRepository>();
builder.Services.AddScoped<Api.Modules.AccessControl.Persistence.Repositories.Attributes.IRoleAttributeRepository, Api.Modules.AccessControl.Persistence.Repositories.Attributes.RoleAttributeRepository>();
builder.Services.AddScoped<Api.Modules.AccessControl.Persistence.Repositories.Audit.IAuditLogRepository, Api.Modules.AccessControl.Persistence.Repositories.Audit.AuditLogRepository>();

// Add management services
builder.Services.AddScoped<IPolicyManagementService, PolicyManagementService>();
builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();
builder.Services.AddScoped<IGroupAttributeManagementService, GroupAttributeManagementService>();
builder.Services.AddScoped<IUserAttributeManagementService, UserAttributeManagementService>();
builder.Services.AddScoped<IResourceManagementService, ResourceManagementService>();
builder.Services.AddScoped<IAbacRuleGroupManagementService, AbacRuleGroupManagementService>();
builder.Services.AddScoped<IAbacRuleManagementService, AbacRuleManagementService>();
builder.Services.AddScoped<IAttributeSchemaManagementService, AttributeSchemaManagementService>();
builder.Services.AddScoped<IRoleAttributeManagementService, RoleAttributeManagementService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IGroupManagementService, GroupManagementService>();

// Add Global Attribute Service (used by DatabaseUserService)
builder.Services.AddScoped<IGlobalAttributeService, GlobalAttributeService>();

// Conditional Graph API service registration based on feature flag
var graphApiEnabled = builder.Configuration.GetValue<bool>("FeatureManagement:GraphApi");

if (graphApiEnabled)
{
    // Graph API enabled: Use real Graph services with caching
    builder.Services.AddScoped<GraphUserService>();
    builder.Services.AddScoped<GraphGroupService>();
    builder.Services.AddScoped<IGraphUserService, CachedGraphUserService>();
    builder.Services.AddScoped<IGraphGroupService, CachedGraphGroupService>();
}
else
{
    // Graph API disabled: Use database-backed services
    builder.Services.AddScoped<IGraphUserService, DatabaseUserService>();
    builder.Services.AddScoped<IGraphGroupService, DatabaseGroupService>();
}

// Add testing services for TestController
builder.Services.AddScoped<ITokenAnalysisService, TokenAnalysisService>();
builder.Services.AddScoped<IAuthorizationTestingService, AuthorizationTestingService>();
builder.Services.AddScoped<IScenarioTestingService, ScenarioTestingService>();

var app = builder.Build();

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var accessControlDbContext = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
    accessControlDbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Production: Use safe error handler that doesn't expose stack traces
    app.UseExceptionHandler("/Home/Error");

    // HSTS: Force HTTPS for security (31536000 seconds = 1 year)
    app.UseHsts();
}
else
{
    // Development: Show detailed error page with stack traces
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();

// Validate token cache on each request (after authentication, before authorization)
// This proactively redirects to sign-in if token cache is empty (e.g., after restart)
app.UseMiddleware<TokenCacheValidationMiddleware>();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
