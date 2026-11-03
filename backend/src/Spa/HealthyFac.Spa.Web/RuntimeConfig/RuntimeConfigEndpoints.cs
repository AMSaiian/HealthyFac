using HealthyFac.Spa.Web.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace HealthyFac.Spa.Web.RuntimeConfig;

public static class RuntimeConfigEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps <c>/config.json</c>. The browser reads it before it starts the app and before any sign-in, so the
        /// response is public. The method maps each field by hand and never serializes the options object.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapRuntimeConfigEndpoints()
        {
            app.MapGet(SpaConstants.RuntimeConfigRoute, GetRuntimeConfig).AllowAnonymous();

            return app;
        }
    }

    private static RuntimeConfigResponse GetRuntimeConfig(HttpContext context, IOptions<SpaOptions> options)
    {
        context.Response.Headers[HeaderNames.CacheControl] = SpaConstants.CacheControl.NoStore;

        return new RuntimeConfigResponse { ApiBaseUrl = options.Value.ApiBaseUrl };
    }
}
