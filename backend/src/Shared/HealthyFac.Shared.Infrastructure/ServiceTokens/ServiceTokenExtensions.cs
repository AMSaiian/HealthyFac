using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Infrastructure.ServiceTokens;

/// <summary>
/// Provides extension methods for the Client Credentials token of a service that calls another API for itself.
/// </summary>
public static class ServiceTokenExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated token options, the client of the token endpoint, the token store in Redis, the
        /// token caches, the handlers, and the holder of the downstream scopes. Call it once for each service. A
        /// client then gets the token with <see cref="AddClientCredentialsToken"/>. The service must register the
        /// Redis connection (<c>AddSharedRedis</c>) and the service name (<c>AddServiceOptions</c>) too, because the
        /// store reads both. A service that already registers them for another use does not register them twice.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddServiceTokens(IConfiguration configuration)
        {
            services.AddSharedTime();
            services.AddValidatedOptions<ServiceTokenOptions, ServiceTokenOptionsValidator>(
                ServiceTokenOptions.SectionName,
                configuration);
            services.AddHttpClient(ServiceTokenConstants.TokenClientName);
            services.TryAddSingleton(provider => new TokenEndpointClient(
                provider.GetRequiredService<IHttpClientFactory>(),
                provider.GetRequiredService<IOptions<ServiceTokenOptions>>(),
                provider.GetRequiredService<TimeProvider>(),
                Random.Shared));
            services.TryAddSingleton<ITokenCacheStorage, RedisTokenCacheStorage>();
            services.TryAddSingleton<TokenStore>();
            services.TryAddSingleton<ServiceTokenCache>();
            services.TryAddTransient<ClientCredentialsHandler>();
            services.TryAddSingleton<TokenExchangeClient>();
            services.TryAddTransient<ActorHeadersHandler>();
            services.TryAddSingleton<IDownstreamScopeHolder, DownstreamScopeHolder>();

            return services;
        }
    }

    extension(IHttpClientBuilder builder)
    {
        /// <summary>
        /// Adds the handler that sets the access token of the service on each request of the client. The service
        /// must call <see cref="AddServiceTokens"/> first.
        /// </summary>
        /// <returns>The <see cref="IHttpClientBuilder"/> instance for chained configuration.</returns>
        public IHttpClientBuilder AddClientCredentialsToken()
            => builder.AddHttpMessageHandler<ClientCredentialsHandler>();

        /// <summary>
        /// Adds the handler that sets the actor headers of the calling flow on each request of the client. Use it
        /// after <see cref="AddClientCredentialsToken"/> for a client that calls one of our own services, and never
        /// for the Keycloak client or an external API. The service must call <see cref="AddServiceTokens"/> first,
        /// and the host must register <see cref="IIncomingAccessTokenAccessor"/>.
        /// </summary>
        /// <returns>The <see cref="IHttpClientBuilder"/> instance for chained configuration.</returns>
        public IHttpClientBuilder AddActorHeaders() => builder.AddHttpMessageHandler<ActorHeadersHandler>();

        /// <summary>
        /// Adds the handler that exchanges the token of the user of the current request for a short token of one
        /// target audience, and sets it on each request of the client. Use it for a call on behalf of a user.
        /// The service must call <see cref="AddServiceTokens"/> first, and the host must register
        /// <see cref="IIncomingAccessTokenAccessor"/>. Keycloak must allow the audience as a client scope of the
        /// calling client. The token carries the permissions of the user. The downstream scopes do not apply to it.
        /// </summary>
        /// <param name="audience">The client ID of the target service.</param>
        /// <param name="scope">The scopes to request, separated by spaces. The handler adds
        /// <see cref="ServiceTokenConstants.OrganizationScope"/>.</param>
        /// <returns>The <see cref="IHttpClientBuilder"/> instance for chained configuration.</returns>
        public IHttpClientBuilder AddUserDelegatedToken(string audience, string scope) =>
            builder.AddHttpMessageHandler(provider => new TokenExchangeHandler(
                provider.GetRequiredService<IIncomingAccessTokenAccessor>(),
                provider.GetRequiredService<TokenExchangeClient>(),
                audience,
                scope));
    }
}
