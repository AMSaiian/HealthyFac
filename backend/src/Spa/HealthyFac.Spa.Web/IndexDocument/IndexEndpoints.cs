using System.Net.Mime;
using HealthyFac.Spa.Web.Hosting;
using HealthyFac.Spa.Web.StaticAssets;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace HealthyFac.Spa.Web.IndexDocument;

public static class IndexEndpoints
{
    private const string CatchAllPattern = "{**path}";
    private const string HtmlContentType = $"{MediaTypeNames.Text.Html}; charset=utf-8";

    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps the index document for the root, for <c>/index.html</c>, and for every other path without a file
        /// extension, so a deep link of the client router loads the app. A path with an extension that no file
        /// matches is a missing file, never the index.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapIndexEndpoints()
        {
            app.MapGet("/", ServeIndex).AllowAnonymous();
            app.MapGet($"/{SpaConstants.IndexFileName}", ServeIndex).AllowAnonymous();
            app.MapFallback(CatchAllPattern, ServeFallback).AllowAnonymous();

            return app;
        }
    }

    private static IResult ServeFallback(HttpContext context, IndexDocument indexDocument, IOptions<SpaOptions> options)
    {
        if (!HttpMethods.IsGet(context.Request.Method) || StaticAssetRequests.IsStaticAsset(context.Request.Path))
        {
            return Results.NotFound();
        }

        return ServeIndex(context, indexDocument, options);
    }

    private static IResult ServeIndex(HttpContext context, IndexDocument indexDocument, IOptions<SpaOptions> options)
    {
        var nonce = CspNonce.Create();
        var headers = context.Response.Headers;
        headers[HeaderNames.ContentSecurityPolicy] =
            ContentSecurityPolicy.Build(
                nonce,
                new Uri(options.Value.ApiBaseUrl),
                new Uri(options.Value.AuthUrl));
        headers[HeaderNames.CacheControl] = SpaConstants.CacheControl.NoCache;
        headers[HeaderNames.XContentTypeOptions] = SpaConstants.SecurityHeaders.ContentTypeOptions;
        headers[SpaConstants.SecurityHeaders.ReferrerPolicyName] = SpaConstants.SecurityHeaders.ReferrerPolicy;
        headers[SpaConstants.SecurityHeaders.PermissionsPolicyName] = options.Value.PermissionsPolicy;

        return Results.Content(indexDocument.Render(nonce), HtmlContentType);
    }
}
