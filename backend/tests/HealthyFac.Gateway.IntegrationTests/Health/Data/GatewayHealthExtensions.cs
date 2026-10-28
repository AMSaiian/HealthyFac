using System.Net;
using System.Text;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace HealthyFac.Gateway.IntegrationTests.Health.Data;

public static class GatewayHealthExtensions
{
    /// <summary>
    /// The port of <c>HealthEndpoints:Port</c> in <c>appsettings.json</c>. The test server has no real ports, so
    /// a request names the local port of its connection.
    /// </summary>
    private const int HealthPort = 8090;

    extension(GatewayFactory gateway)
    {
        public async Task<HealthResponse> GetOnHealthPortAsync(string route)
        {
            var context = await gateway.Server.SendAsync(httpContext =>
            {
                httpContext.Request.Method = HttpMethods.Get;
                httpContext.Request.Path = route;
                httpContext.Connection.LocalPort = HealthPort;
            });

            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            return new HealthResponse((HttpStatusCode)context.Response.StatusCode, body);
        }
    }
}

public sealed record HealthResponse(HttpStatusCode StatusCode, string Body);
