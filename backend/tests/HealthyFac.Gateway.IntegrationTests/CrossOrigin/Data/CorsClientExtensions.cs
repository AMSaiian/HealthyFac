using HealthyFac.Gateway.IntegrationTests.Hosting;

namespace HealthyFac.Gateway.IntegrationTests.CrossOrigin.Data;

public static class CorsClientExtensions
{
    private const string OriginHeader = "Origin";
    private const string RequestMethodHeader = "Access-Control-Request-Method";
    private const string RequestHeadersHeader = "Access-Control-Request-Headers";

    extension(HttpClient client)
    {
        /// <summary>Sends a browser preflight: an <c>OPTIONS</c> request without a token.</summary>
        /// <param name="route">The request path.</param>
        /// <param name="requestedMethod">The method that the browser asks to use.</param>
        /// <returns>The response to the preflight.</returns>
        public Task<HttpResponseMessage> SendPreflightAsync(string route, string requestedMethod = "POST")
        {
            var request = new HttpRequestMessage(GatewayCorsTestData.PreflightMethod, route);
            request.Headers.Add(OriginHeader, GatewayFactory.DefaultCorsOrigin);
            request.Headers.Add(RequestMethodHeader, requestedMethod);
            request.Headers.Add(RequestHeadersHeader, GatewayCorsTestData.RequestHeaders);

            return client.SendAsync(request);
        }

        public Task<HttpResponseMessage> GetFromOriginAsync(string route)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, route);
            request.Headers.Add(OriginHeader, GatewayFactory.DefaultCorsOrigin);

            return client.SendAsync(request);
        }
    }
}
