using System.Net;
using System.Net.Http.Json;

namespace HealthyFac.Insights.UnitTests.WorkOrders.Data;

/// <summary>
/// Plays the token endpoint of Keycloak. It answers one fixed service token.
/// </summary>
public sealed class FakeTokenEndpointHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(
                    new { access_token = CoreClientRegistrationTestData.ServiceToken, expires_in = 300 }),
            });
}
