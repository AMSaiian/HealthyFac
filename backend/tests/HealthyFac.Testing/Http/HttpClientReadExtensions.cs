using System.Net.Http.Json;
using HealthyFac.Shared.Infrastructure.HttpFailures;

namespace HealthyFac.Testing.Http;

/// <summary>
/// Reads a response body in a test and fails with the reason of the service when the status is not a success. The
/// <c>HttpClient</c> methods that check the status inside are banned (<c>BannedSymbols.txt</c>).
/// </summary>
public static class HttpClientReadExtensions
{
    extension(HttpClient client)
    {
        /// <summary>Sends a <c>GET</c> request and reads the JSON body.</summary>
        /// <typeparam name="TValue">The type of the body.</typeparam>
        /// <param name="path">The path or the address of the request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
        /// requests.</param>
        /// <returns>The body, or <see langword="null"/> when it is the JSON <c>null</c>.</returns>
        public async Task<TValue?> GetJsonAsync<TValue>(string path, CancellationToken cancellationToken)
        {
            using var response = await client.GetAsync(path, cancellationToken);
            await response.EnsureSuccessAsync(cancellationToken);

            return await response.Content.ReadFromJsonAsync<TValue>(cancellationToken);
        }

        /// <summary>Sends a <c>GET</c> request and reads the text body.</summary>
        /// <param name="path">The path or the address of the request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
        /// requests.</param>
        /// <returns>The body.</returns>
        public async Task<string> GetTextAsync(string path, CancellationToken cancellationToken)
        {
            using var response = await client.GetAsync(path, cancellationToken);
            await response.EnsureSuccessAsync(cancellationToken);

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
    }
}
