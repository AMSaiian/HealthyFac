using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Shared.UnitTests.CrossOrigin.Data;

public static class CorsClientExtensions
{
    private const string OriginHeader = "Origin";
    private const string RequestMethodHeader = "Access-Control-Request-Method";
    private const string RequestHeadersHeader = "Access-Control-Request-Headers";

    extension(IHost host)
    {
        /// <summary>Sends a browser request with an <c>Origin</c> header and no token.</summary>
        /// <param name="path">The request path.</param>
        /// <param name="origin">The value of the <c>Origin</c> header.</param>
        /// <returns>The response to the request.</returns>
        public async Task<HttpResponseMessage> SendCrossOriginPostAsync(string path, string origin)
        {
            using var client = host.GetTestClient();
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Add(OriginHeader, origin);

            return await client.SendAsync(request);
        }

        /// <summary>Sends a browser preflight: an <c>OPTIONS</c> request without a token.</summary>
        /// <param name="path">The request path.</param>
        /// <param name="origin">The value of the <c>Origin</c> header.</param>
        /// <returns>The response to the preflight.</returns>
        public async Task<HttpResponseMessage> SendPreflightAsync(string path, string origin)
        {
            using var client = host.GetTestClient();
            var request = new HttpRequestMessage(HttpMethod.Options, path);
            request.Headers.Add(OriginHeader, origin);
            request.Headers.Add(RequestMethodHeader, HttpMethod.Post.Method);
            request.Headers.Add(RequestHeadersHeader, value: "authorization,content-type");

            return await client.SendAsync(request);
        }
    }
}
