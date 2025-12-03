using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.BusinessEvents;
using Api.Modules.AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
