using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Shared.UnitTests.HealthChecks.Data;

public static class HealthRequestExtensions
{
    extension(IHost host)
    {
        /// <summary>
        /// Sends a GET request as if it arrived on a local port. <see cref="TestServer"/> has no real ports, so the
        /// request sets the local port of the connection. The health middleware compares that port.
        /// </summary>
        /// <param name="path">The request path.</param>
        /// <param name="localPort">The local port of the connection.</param>
        /// <param name="hostHeader">The <c>Host</c> header, or <see langword="null"/> for the default.</param>
        /// <returns>The status code and the body of the response.</returns>
        public async Task<HealthResponse> GetOnPortAsync(string path, int localPort, string? hostHeader = null)
        {
            var context = await host.GetTestServer()
                .SendAsync(httpContext =>
                {
                    httpContext.Request.Method = HttpMethods.Get;
                    httpContext.Request.Path = path;
                    httpContext.Connection.LocalPort = localPort;

                    if (hostHeader is not null)
                    {
                        httpContext.Request.Host = new HostString(hostHeader);
                    }
                });

            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            return new HealthResponse(context.Response.StatusCode, body);
        }
    }
}

public sealed record HealthResponse(int StatusCode, string Body);
