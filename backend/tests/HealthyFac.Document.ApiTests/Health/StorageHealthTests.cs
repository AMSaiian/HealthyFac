using System.Diagnostics;
using System.Net;
using HealthyFac.Document.ApiTests.Health.Data;
using HealthyFac.Document.ApiTests.Hosting;
using HealthyFac.Testing.Features;

namespace HealthyFac.Document.ApiTests.Health;

[Collection(DocumentApiCollection.Name)]
[Feature(TestFeatures.Platform)]
public sealed class StorageHealthTests(DocumentApiFixture fixture)
{
    private const string ReadinessRoute = "/health/ready";

    private readonly DocumentApiFixture _fixture = fixture;

    [Fact]
    public async Task HealthReady_WithStorageRunning_ReturnsHealthy()
    {
        await using var factory = _fixture.CreateFactoryWithStorage(_fixture.StorageConnectionString);

        var response = await factory.GetOnHealthPortAsync(ReadinessRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Should().Be(expected: "Healthy");
    }

    [Fact]
    public async Task HealthReady_WithStorageDown_ReturnsDegradedWith200()
    {
        await using var factory =
            _fixture.CreateFactoryWithFailingStorage(DocumentApiTestData.UnreachableStorageConnection);

        var response = await factory.GetOnHealthPortAsync(ReadinessRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Should().Be(expected: "Degraded");
    }

    [Fact]
    public async Task HealthReady_WithHangingStorage_AnswersBeforeTheProbeTimeout()
    {
        await using var factory =
            _fixture.CreateFactoryWithFailingStorage(DocumentApiTestData.HangingStorageConnection);

        await factory.Server.SendAsync(_ => { }, TestContext.Current.CancellationToken);
        var stopwatch = Stopwatch.StartNew();

        var response = await factory.GetOnHealthPortAsync(ReadinessRoute);

        stopwatch.Stop();
        response.Body.Should().Be(expected: "Degraded");
        stopwatch.Elapsed.Should().BeLessThan(DocumentApiTestData.ReadinessProbeTimeout);
    }
}
