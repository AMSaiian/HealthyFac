using System.Threading.RateLimiting;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using RedisRateLimiting;
using StackExchange.Redis;

namespace HealthyFac.Gateway.Web.RateLimiting;

/// <summary>
/// Provides extension methods for registering the rate limiter policies.
/// </summary>
public static class RateLimitingExtensions
{
    private const string UnknownClient = "unknown";

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated rate limit options and one fixed window policy per route kind. Each policy
        /// counts in Redis, so every gateway instance shares one limit, and falls back to a local counter when
        /// Redis fails. The policies partition by the connection address, which only a trusted proxy can change
        /// through the forwarded headers middleware.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddGatewayRateLimiting(IConfiguration configuration)
        {
            services.AddValidatedOptions<RateLimitOptions, RateLimitOptionsValidator>(
                RateLimitOptions.SectionName,
                configuration);

            services.AddSingleton<RateLimitRejectionHandler>();
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = (context, cancellationToken) => context.HttpContext.RequestServices
                    .GetRequiredService<RateLimitRejectionHandler>()
                    .HandleAsync(context, cancellationToken);

                options.AddPolicy(
                    RateLimitPolicyNames.AnonymousStrict,
                    context => PartitionByClient(
                        context,
                        RateLimitPolicyNames.AnonymousStrict,
                        limits => limits.Strict));

                options.AddPolicy(
                    RateLimitPolicyNames.Default,
                    context => PartitionByClient(
                        context,
                        RateLimitPolicyNames.Default,
                        limits => limits.Default));
            });

            return services;
        }
    }

    private static RateLimitPartition<string> PartitionByClient(
        HttpContext context,
        string policyName,
        Func<RateLimitOptions, FixedWindowLimit> selectLimit
    )
    {
        var services = context.RequestServices;
        var limit = selectLimit(services.GetRequiredService<IOptions<RateLimitOptions>>().Value);
        var clientAddress = context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient;
        var window = TimeSpan.FromSeconds(limit.WindowSeconds);

        return RateLimitPartition.Get(
            $"{policyName}:{clientAddress}",
            partitionKey => new FallbackRateLimiter(
                new RedisFixedWindowRateLimiter<string>(
                    partitionKey,
                    new RedisFixedWindowRateLimiterOptions
                    {
                        ConnectionMultiplexerFactory = services.GetRequiredService<IConnectionMultiplexer>,
                        PermitLimit = limit.PermitLimit,
                        Window = window,
                    }),
                () => new FixedWindowRateLimiter(
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limit.PermitLimit,
                        Window = window,
                        QueueLimit = 0,
                        AutoReplenishment = false,
                    }),
                window,
                services.GetRequiredService<RedisCircuitBreaker>()));
    }
}
