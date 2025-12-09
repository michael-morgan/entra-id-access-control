using Api.Modules.DemoApi.Authorization;
using Api.Modules.DemoApi.Data;
using Api.Modules.DemoApi.Services.Claims;
using Api.Modules.DemoApi.Services.Documents;
using Api.Modules.DemoApi.Services.Loans;
using Api.Modules.AccessControl.Interfaces;
using Api.Modules.AccessControl;
using Api.Modules.AccessControl.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add FluentValidation for input validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add Problem Details for standardized API error responses (RFC 7807)
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        // Don't expose exception details in production
        if (!context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            context.ProblemDetails.Extensions.Remove("exception");
            context.ProblemDetails.Extensions.Remove("exceptionType");
            context.ProblemDetails.Extensions.Remove("exceptionMessage");
        }
    };
});

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

// Add rate limiting to protect against abuse (100 requests/minute per IP)
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            ipAddress,
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Add CORS with specific allowed origins (production-ready)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        // Get allowed origins from configuration (comma-separated)
        var allowedOrigins = builder.Configuration.GetValue<string>("Cors:AllowedOrigins")
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? new[] { "https://localhost:7006", "http://localhost:5146" }; // Default for development

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Allow cookies/auth headers
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Configure error handling
if (!app.Environment.IsDevelopment())
{
    // Production: Use problem details for API errors (RFC 7807)
    app.UseExceptionHandler(options => { });
    app.UseHsts();
}
else
{
    // Development: Show detailed errors and Swagger
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Access Control Demo API v1");
    });
}

app.UseHttpsRedirection();

// Apply rate limiting before other middleware
app.UseRateLimiter();

app.UseCors("AllowSpecificOrigins");

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
