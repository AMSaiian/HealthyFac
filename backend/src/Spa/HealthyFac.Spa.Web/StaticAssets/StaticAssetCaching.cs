using System.Globalization;
using System.Text.RegularExpressions;
using HealthyFac.Spa.Web.Hosting;
using HealthyFac.Spa.Web.IndexDocument.Csp;
using HealthyFac.Spa.Web.IndexDocument;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace HealthyFac.Spa.Web.StaticAssets;

public static class StaticAssetCaching
{
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Serves the files of the content root. A file with an Angular content hash in its name never changes, so
        /// it caches for the configured time. Every other file (e.g., translations, the icon) caches only after the browser
        /// checks it.
        /// </summary>
        /// <returns>The <see cref="IApplicationBuilder"/> instance for chained configuration.</returns>
        public IApplicationBuilder UseSpaStaticFiles()
        {
            var options = app.ApplicationServices.GetRequiredService<IOptions<SpaOptions>>().Value;
            var contentRoot = Path.GetFullPath(options.ContentRoot, AppContext.BaseDirectory);
            var hashedFileName = new Regex(
                options.HashedFileNamePattern,
                RegexOptions.CultureInvariant,
                SpaConstants.HashedFiles.PatternMatchTimeout);
            var immutableCacheControl = string.Format(
                CultureInfo.InvariantCulture,
                SpaConstants.CacheControl.ImmutableFormat,
                options.ImmutableCacheSeconds);

            return app.UseStaticFiles(
                new StaticFileOptions
                {
                    FileProvider = new EndpointFilesExcludedProvider(new PhysicalFileProvider(contentRoot)),
                    OnPrepareResponse = context =>
                    {
                        var headers = context.Context.Response.Headers;
                        headers[HeaderNames.CacheControl] = hashedFileName.IsMatch(context.File.Name)
                            ? immutableCacheControl
                            : SpaConstants.CacheControl.NoCache;
                        if (context.File.Name == SpaConstants.SilentSsoFileName)
                        {
                            headers[HeaderNames.ContentSecurityPolicy] = ContentSecurityPolicy.SilentSso;
                        }
                    },
                });
        }
    }
}
