using HealthyFac.Seeder.IntegrationTests.FullSeed.Data;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.TelemetryHistory.Data;
using HealthyFac.Seeder.IntegrationTests.Timeline.Data;

namespace HealthyFac.Seeder.IntegrationTests.FullSeed;

[Collection(SeederCollection.Name)]
public sealed class TelemetryModeTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact(Explicit = true)]
    public async Task RunAsync_WithResetAndTelemetryOnAModeOneSeed_SeedsModeTwoWithTheMarker()
    {
        var baseDirectory = TelemetryHistorySeedTestData.CreateBaseDirectory(TelemetryHistorySeedTestData.TestScript);

        var first = await _fixture.RunJobAsync(
            [],
            TestContext.Current.CancellationToken,
            baseDirectory,
            anchor: TelemetryHistorySeedTestData.ThursdayAnchor);

        var hasHistoryBefore = await _fixture.HasReceivedMessageAsync(TelemetryHistorySeedTestData.MarkerMessageId);

        var reset = await _fixture.RunJobAsync(
            [TelemetryHistorySeedTestData.ResetArgument, TelemetryHistorySeedTestData.TelemetryArgument],
            TestContext.Current.CancellationToken,
            baseDirectory,
            anchor: TelemetryHistorySeedTestData.ThursdayAnchor.AddDays(FullSeedTestData.LaterAnchorDays));

        var hasHistoryAfter = await _fixture.HasReceivedMessageAsync(TelemetryHistorySeedTestData.MarkerMessageId);

        first.ExitCode.Should().Be(expected: 0);
        hasHistoryBefore.Should().BeFalse();
        reset.ExitCode.Should().Be(expected: 0);
        hasHistoryAfter.Should().BeTrue();
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_ModeTwoOnACompleteModeTwoSeed_PrintsCompleteAndExitsWithZero()
    {
        var baseDirectory = await SeedModeTwoAsync();

        var job = await _fixture.RunJobAsync(
            [TelemetryHistorySeedTestData.TelemetryArgument],
            TestContext.Current.CancellationToken,
            baseDirectory,
            anchor: TelemetryHistorySeedTestData.ThursdayAnchor);

        job.ExitCode.Should().Be(expected: 0);
        job.Output.Should().Contain(SeedCompletionTestData.CompleteText);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_ModeOneOnACompleteModeTwoSeed_PrintsCompleteAndExitsWithZero()
    {
        var baseDirectory = await SeedModeTwoAsync();

        var job = await _fixture.RunJobAsync(
            [],
            TestContext.Current.CancellationToken,
            baseDirectory,
            anchor: TelemetryHistorySeedTestData.ThursdayAnchor);

        job.ExitCode.Should().Be(expected: 0);
        job.Output.Should().Contain(SeedCompletionTestData.CompleteText);
    }

    private async Task<string> SeedModeTwoAsync()
    {
        var baseDirectory = TelemetryHistorySeedTestData.CreateBaseDirectory(TelemetryHistorySeedTestData.TestScript);

        await _fixture.RunJobAsync(
            [TelemetryHistorySeedTestData.TelemetryArgument],
            TestContext.Current.CancellationToken,
            baseDirectory,
            anchor: TelemetryHistorySeedTestData.ThursdayAnchor);

        return baseDirectory;
    }
}
