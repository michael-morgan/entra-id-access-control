using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.BusinessEvents;
using Api.Modules.AccessControl.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using UI.Modules.AccessControl.Middleware;
using UI.Modules.AccessControl.Services;

var builder = WebApplication.CreateBuilder(args);

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

// Add session support for workstream context
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add unified database context
builder.Services.AddDbContext<AccessControlDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AccessControlDb")));

// Add business event query service
builder.Services.AddScoped<IBusinessEventQueryService, BusinessEventQueryService>();

// Add Graph API services
builder.Services.AddScoped<GraphUserService>();
builder.Services.AddScoped<GraphGroupService>();

var app = builder.Build();

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var authDbContext = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
    authDbContext.Database.Migrate();

    var eventsDbContext = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
    eventsDbContext.Database.Migrate();

    var auditDbContext = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
    auditDbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
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
