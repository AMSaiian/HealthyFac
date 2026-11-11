using System.Diagnostics;
using System.Net;
using HealthyFac.Insights.ApiTests.Health.Data;
using HealthyFac.Insights.ApiTests.Hosting;
using HealthyFac.Insights.Infrastructure.Conversion;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.ApiTests.Health;

[Collection(InsightsApiCollection.Name)]
public sealed class DoclingHealthTests(InsightsApiFixture fixture)
{
    private const string ReadinessRoute = "/health/ready";
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(seconds: 3);

    private readonly InsightsApiFixture _fixture = fixture;

    [Fact]
    public async Task HealthReady_WithDoclingAnswering_ReturnsHealthy()
    {
        await using var factory = CreateFactory(new FakeDoclingHealthHandler { Status = HttpStatusCode.OK });

        var response = await factory.GetOnHealthPortAsync(ReadinessRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Should().Be(expected: "Healthy");
    }

    [Fact]
    public async Task HealthReady_WithDoclingDown_ReturnsDegradedWith200()
    {
        await using var factory = CreateFactory(new FakeDoclingHealthHandler());

        var response = await factory.GetOnHealthPortAsync(ReadinessRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Should().Be(expected: "Degraded");
    }

    [Fact]
    public async Task HealthReady_WithDoclingAnsweringAnError_ReturnsDegradedWith200()
    {
        await using var factory = CreateFactory(
            new FakeDoclingHealthHandler { Status = HttpStatusCode.InternalServerError });

        var response = await factory.GetOnHealthPortAsync(ReadinessRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Should().Be(expected: "Degraded");
    }

    [Fact]
    public async Task HealthReady_WithHangingDocling_AnswersBeforeTheProbeTimeout()
    {
        await using var factory = CreateFactory(new FakeDoclingHealthHandler { IsHanging = true });
        await factory.Server.SendAsync(_ => { }, TestContext.Current.CancellationToken);
        var stopwatch = Stopwatch.StartNew();

        var response = await factory.GetOnHealthPortAsync(ReadinessRoute);

        stopwatch.Stop();
        response.Body.Should().Be(expected: "Degraded");
        stopwatch.Elapsed.Should().BeLessThan(ProbeTimeout);
    }

    private InsightsApiFactory CreateFactory(FakeDoclingHealthHandler handler) =>
        _fixture.CreateFactory(services =>
            services.AddHttpClient(DoclingConstants.ClientName).ConfigurePrimaryHttpMessageHandler(() => handler));
}
