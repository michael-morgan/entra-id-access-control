using Api.Modules.DemoApi.Authorization;
using Api.Modules.DemoApi.Data;
using Api.Modules.DemoApi.Services.Claims;
using Api.Modules.DemoApi.Services.Documents;
using Api.Modules.DemoApi.Services.Loans;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl.AspNetCore;
using Api.Modules.AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Access Control Demo API",
        Version = "v1",
        Description = "Enterprise Access Control Framework - Proof of Concept with Loans, Claims, and Documents workstreams"
    });
});

// Add Access Control Framework (includes correlation, authorization, events, audit)
builder.Services.AddAccessControlFramework(builder.Configuration);

// Add Entra ID authentication
builder.Services.AddEntraIdAuthentication(builder.Configuration);

// Add workstream repositories
builder.Services.AddSingleton<ILoanRepository, InMemoryLoanRepository>();
builder.Services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();
builder.Services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();

// Add workstream services
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

// Register ABAC evaluators
builder.Services.AddSingleton<IWorkstreamAbacEvaluator, LoansAbacEvaluator>();

// Add CORS for local development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Access Control Demo API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Use Access Control Framework middleware (correlation) - MUST be before auth
app.UseAccessControlFramework();

// Initialize ABAC functions with service provider
app.UseAccessControlAbac();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply pending migrations at startup
await ApplyMigrations(app.Services);

// Seed data if requested
if (args.Contains("--seed"))
{
    await SeedData(app.Services);
}

app.Run();

static async Task ApplyMigrations(IServiceProvider services)
{
    Console.WriteLine("=== Applying Database Migrations ===");

    using var scope = services.CreateScope();
    var sp = scope.ServiceProvider;

    var authContext = sp.GetRequiredService<AccessControlDbContext>();
    var pendingMigrations = await authContext.Database.GetPendingMigrationsAsync();

    if (pendingMigrations.Any())
    {
        Console.WriteLine($"Found {pendingMigrations.Count()} pending migration(s):");
        foreach (var migration in pendingMigrations)
        {
            Console.WriteLine($"  - {migration}");
        }

        await authContext.Database.MigrateAsync();
        Console.WriteLine("✓ Migrations applied successfully");
    }
    else
    {
        Console.WriteLine("✓ Database is up to date");
    }
}

static async Task SeedData(IServiceProvider services)
{
    Console.WriteLine("=== Seeding Initial Data ===");

    using var scope = services.CreateScope();
    var sp = scope.ServiceProvider;

    var authContext = sp.GetRequiredService<AccessControlDbContext>();
    var seedService = new SeedDataService(authContext);

    await seedService.SeedAsync();

    Console.WriteLine("=== Seed Data Completed ===");
}
