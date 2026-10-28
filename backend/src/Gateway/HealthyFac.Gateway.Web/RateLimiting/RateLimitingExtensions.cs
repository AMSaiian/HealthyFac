using System.Threading.RateLimiting;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using RedisRateLimiting;
using StackExchange.Redis;

namespace HealthyFac.Gateway.Web.RateLimiting;

/// <summary>
/// Provides extension methods for registering the rate limiter policies.
/// </summary>
public static class RateLimitingExtensions
{
    private const string UnknownClient = "unknown";
    private const string AddressKeyPrefix = "ip:";
    private const string UserKeyPrefix = "user:";

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
                        limits => limits.Strict,
                        ClientAddress));

                options.AddPolicy(
                    RateLimitPolicyNames.Default,
                    context => PartitionByClient(
                        context,
                        RateLimitPolicyNames.Default,
                        limits => limits.Default,
                        UserOrClientAddress));
            });

            return services;
        }
    }

    private static string ClientAddress(HttpContext context) =>
        $"{AddressKeyPrefix}{context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient}";

    private static string UserOrClientAddress(HttpContext context)
    {
        var subject = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return context.User.Identity?.IsAuthenticated == true && subject is not null
            ? $"{UserKeyPrefix}{subject}"
            : ClientAddress(context);
    }

    private static RateLimitPartition<string> PartitionByClient(
        HttpContext context,
        string policyName,
        Func<RateLimitOptions, FixedWindowLimit> selectLimit,
        Func<HttpContext, string> selectClient
    )
    {
        var services = context.RequestServices;
        var limit = selectLimit(services.GetRequiredService<IOptions<RateLimitOptions>>().Value);
        var client = selectClient(context);
        var window = TimeSpan.FromSeconds(limit.WindowSeconds);

        return RateLimitPartition.Get(
            $"{policyName}:{client}",
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
