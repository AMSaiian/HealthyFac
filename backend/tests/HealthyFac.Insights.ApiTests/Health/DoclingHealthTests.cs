using System.Diagnostics;
using System.Net;
using HealthyFac.Insights.ApiTests.Health.Data;
using HealthyFac.Insights.ApiTests.Hosting;
using HealthyFac.Insights.Infrastructure.Conversion;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.ApiTests.Health;

[Collection(InsightsApiCollection.Name)]
[Feature(TestFeatures.Platform)]
public sealed class DoclingHealthTests(InsightsApiFixture fixture)
{
    private const string ReadinessRoute = "/health/ready";
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(seconds: 3);

    private readonly InsightsApiFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(DoclingHealthTestData.Answers), MemberType = typeof(DoclingHealthTestData))]
    public async Task HealthReady_WithDoclingAnswer_ReturnsItsStateWith200(
        string caseName,
        Func<FakeDoclingHealthHandler> createHandler,
        string expectedBody
    )
    {
        await using var factory = CreateFactory(createHandler());

        var response = await factory.GetOnHealthPortAsync(ReadinessRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Should().Be(expectedBody, caseName);
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
