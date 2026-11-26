using System.Net;
using System.Net.Mime;
using System.Text;
using HealthyFac.Gateway.IntegrationTests.Composition.Data;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;

namespace HealthyFac.Gateway.IntegrationTests.Composition;

[Collection(GatewayLimitsCollection.Name)]
public sealed class CompositionRateLimitTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private const string MachineSearchBody = """{"offset":0,"limit":25,"filter":{}}""";

    private readonly GatewayEnvironment _environment = environment;
    private readonly CompositionStubs _stubs = new();

    public async ValueTask InitializeAsync() => await _stubs.StartAsync();

    public async ValueTask DisposeAsync() => await _stubs.DisposeAsync();

    [Fact]
    public async Task ComposedEndpoint_AboveTheDefaultLimit_Returns429AfterTheLimitIsServed()
    {
        await using var gateway = _stubs.CreateGateway(_environment, RateLimitTestData.DefaultLimitOfTwo);
        using var client = gateway.CreateAuthenticatedClient();
        var statusCodes = new List<HttpStatusCode>();

        for (var attempt = 0; attempt <= RateLimitTestData.DefaultPermitLimit; attempt++)
        {
            using var content = new StringContent(MachineSearchBody, Encoding.UTF8, MediaTypeNames.Application.Json);

            using var response = await client.PostAsync(
                CompositionStubs.PublicMachineSearch, content, TestContext.Current.CancellationToken);

            statusCodes.Add(response.StatusCode);
        }

        statusCodes.Take(RateLimitTestData.DefaultPermitLimit)
            .Should()
            .OnlyContain(status => status == HttpStatusCode.OK);

        statusCodes[^1].Should().Be(HttpStatusCode.TooManyRequests);
    }
}
