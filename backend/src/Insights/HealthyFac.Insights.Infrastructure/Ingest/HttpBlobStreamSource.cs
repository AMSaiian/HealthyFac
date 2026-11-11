using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Shared.Infrastructure.HttpFailures;

namespace HealthyFac.Insights.Infrastructure.Ingest;

/// <summary>
/// Opens a file from its signed link. The call reads the response headers only, and the body streams on demand, so the
/// worker holds no whole file in memory. The link carries its own signature, so the client sends no token.
/// </summary>
/// <param name="httpClientFactory">Creates the blob client.</param>
public sealed class HttpBlobStreamSource(IHttpClientFactory httpClientFactory) : IBlobStreamSource
{
    public const string ClientName = "blob";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<Stream> OpenAsync(string url, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        try
        {
            await response.EnsureSuccessAsync(cancellationToken);
            var body = await response.Content.ReadAsStreamAsync(cancellationToken);

            return new ResponseBodyStream(body, response);
        }
        catch
        {
            response.Dispose();

            throw;
        }
    }
}
