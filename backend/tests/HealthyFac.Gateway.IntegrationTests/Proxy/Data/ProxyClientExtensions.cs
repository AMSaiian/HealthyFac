using System.Diagnostics;
using System.Net;

namespace HealthyFac.Gateway.IntegrationTests.Proxy.Data;

public static class ProxyClientExtensions
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    extension(HttpClient client)
    {
        /// <summary>
        /// Sends GET requests until one answers with the expected status code, or the wait limit passes.
        /// </summary>
        /// <param name="route">The route to request.</param>
        /// <param name="statusCode">The status code to wait for.</param>
        /// <param name="waitLimit">The longest time to wait.</param>
        /// <returns>A task that completes when the status code appears.</returns>
        /// <exception cref="TimeoutException">Thrown when the status code does not appear in time.</exception>
        public async Task WaitForStatusAsync(string route, HttpStatusCode statusCode, TimeSpan waitLimit)
        {
            var startedAt = Stopwatch.GetTimestamp();
            while (Stopwatch.GetElapsedTime(startedAt) < waitLimit)
            {
                using var response = await client.GetAsync(route);
                if (response.StatusCode == statusCode)
                {
                    return;
                }

                await Task.Delay(PollInterval);
            }

            throw new TimeoutException($"Route {route} did not answer {(int)statusCode} within {waitLimit}.");
        }
    }
}
