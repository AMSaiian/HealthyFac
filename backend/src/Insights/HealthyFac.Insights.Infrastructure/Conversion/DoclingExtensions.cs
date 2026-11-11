using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Conversion;

/// <summary>
/// Provides extension methods for registering the Docling converter.
/// </summary>
public static class DoclingExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated Docling options, the named HTTP client with the API key, and the converter. The
        /// client has no time limit of its own: the cancellation token of the run limits every call.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddDoclingConverter(IConfiguration configuration)
        {
            services.AddValidatedOptions<DoclingOptions, DoclingOptionsValidator>(
                DoclingOptions.SectionName,
                configuration);

            services.AddHttpClient(
                DoclingConstants.ClientName,
                (provider, client) =>
                {
                    var options = provider.GetRequiredService<IOptions<DoclingOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    client.DefaultRequestHeaders.Add(DoclingConstants.ApiKeyHeader, options.ApiKey);
                });

            services.AddSingleton<IDocumentConverter, DoclingDocumentConverter>();

            return services;
        }
    }
}
