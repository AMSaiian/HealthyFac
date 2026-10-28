using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HealthyFac.Shared.Web.Authentication;

/// <summary>
/// Provides extension methods for JWT authentication with Keycloak and for permission policies.
/// </summary>
public static class AuthenticationExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers JWT bearer authentication for the access tokens of Keycloak. The service validates the
        /// signature, the issuer, the audience, and the lifetime itself, and reads the signing keys from the metadata
        /// address. The permissions of the user come from the flat <c>permissions</c> claim, which a Keycloak mapper
        /// writes.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedJwtAuthentication(IConfiguration configuration)
        {
            services.AddValidatedOptions<JwtOptions, JwtOptionsValidator>(JwtOptions.SectionName, configuration);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
                {
                    bearer.MapInboundClaims = false;
                    bearer.MetadataAddress = jwt.Value.MetadataAddress;
                    bearer.RequireHttpsMetadata = jwt.Value.IsHttpsMetadataRequired;
                    bearer.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = jwt.Value.Issuer,
                        ValidAudience = jwt.Value.Audience,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                    };
                });

            return services;
        }
    }

    extension(AuthorizationBuilder builder)
    {
        /// <summary>
        /// Adds one policy for each permission. The policy has the name of the permission and requires a
        /// signed-in user with the matching value in the <c>permissions</c> claim.
        /// </summary>
        /// <param name="permissions">Every permission of the service, for example <c>employees:invite</c>.</param>
        /// <returns>The <see cref="AuthorizationBuilder"/> instance for method chaining.</returns>
        public AuthorizationBuilder AddPermissionPolicies(IReadOnlySet<string> permissions)
        {
            foreach (var permission in permissions)
            {
                builder.AddPolicy(
                    permission,
                    policy => policy
                        .RequireAuthenticatedUser()
                        .RequireClaim(KeycloakClaimTypes.Permissions, permission));
            }

            return builder;
        }

        /// <summary>
        /// Requires a signed-in user for an endpoint or a route without authorization metadata, and for a request
        /// that matches no endpoint. A public endpoint opts out with <c>AllowAnonymous()</c>, and a public YARP
        /// route with <c>"AuthorizationPolicy": "anonymous"</c>.
        /// </summary>
        /// <returns>The <see cref="AuthorizationBuilder"/> instance for method chaining.</returns>
        public AuthorizationBuilder SetAuthenticatedFallbackPolicy() =>
            builder.SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
    }
}
