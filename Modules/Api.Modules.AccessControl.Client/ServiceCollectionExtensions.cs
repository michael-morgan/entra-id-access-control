using Api.Modules.AccessControl.Client.Authorization;
using Api.Modules.AccessControl.Client.Caching;
using Api.Modules.AccessControl.Client.Configuration;
using Api.Modules.AccessControl.Client.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Api.Modules.AccessControl.Client;

/// <summary>
/// Extension methods for registering the AccessControl client SDK.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the AccessControl client SDK with all required services.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAccessControlClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<AccessControlClientOptions>(
            configuration.GetSection(AccessControlClientOptions.SectionName));

        var options = configuration
            .GetSection(AccessControlClientOptions.SectionName)
            .Get<AccessControlClientOptions>();

        if (options == null)
        {
            throw new InvalidOperationException(
                $"AccessControl client configuration not found. " +
                $"Ensure {AccessControlClientOptions.SectionName} section exists in appsettings.json");
        }

        if (string.IsNullOrWhiteSpace(options.ApiBaseUrl))
        {
            throw new InvalidOperationException(
                $"AccessControl:Client:ApiBaseUrl is required");
        }

        // Register HttpContextAccessor (required for JWT forwarding)
        services.AddHttpContextAccessor();

        // Register HTTP client with Polly retry policy
        services.AddHttpClient<AccessControlClient>(client =>
        {
            client.BaseAddress = new Uri(options.ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy(options));

        // Register client without caching
        if (!options.EnableCaching)
        {
            services.AddScoped<IAccessControlClient, AccessControlClient>();
            return services;
        }

        // Register with Redis caching
        if (string.IsNullOrWhiteSpace(options.RedisConnectionString))
        {
            throw new InvalidOperationException(
                "AccessControl:Client:RedisConnectionString is required when EnableCaching is true");
        }

        services.AddStackExchangeRedisCache(redisOptions =>
        {
            redisOptions.Configuration = options.RedisConnectionString;
            redisOptions.InstanceName = "AccessControl:";
        });

        // Register inner client (without caching)
        services.AddScoped<AccessControlClient>();

        // Register cached client as the implementation
        services.AddScoped<IAccessControlClient, CachedAccessControlClient>(sp =>
        {
            var innerClient = sp.GetRequiredService<AccessControlClient>();
            var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AccessControlClientOptions>>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedAccessControlClient>>();

            return new CachedAccessControlClient(innerClient, cache, opts, logger);
        });

        // Register authorization components
        services.AddSingleton<IAuthorizationPolicyProvider, AccessControlPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, ResourceAuthorizationHandler>();

        return services;
    }

    /// <summary>
    /// Creates a Polly retry policy with exponential backoff.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(AccessControlClientOptions options)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // 5xx and 408
            .WaitAndRetryAsync(
                options.RetryCount,
                retryAttempt => TimeSpan.FromMilliseconds(
                    options.RetryDelayMilliseconds * Math.Pow(2, retryAttempt - 1)
                )
            );
    }
}
