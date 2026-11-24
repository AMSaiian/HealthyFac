using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.TelemetryHistory.Data;
using HealthyFac.Seeder.IntegrationTests.Timeline.Data;

namespace HealthyFac.Seeder.IntegrationTests.FullSeed;

[Collection(SeederCollection.Name)]
public sealed class TelemetryModeTests(SeederFixture fixture)
{
    private const int LaterAnchorDays = 7;

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
            anchor: TelemetryHistorySeedTestData.ThursdayAnchor.AddDays(LaterAnchorDays));

        var hasHistoryAfter = await _fixture.HasReceivedMessageAsync(TelemetryHistorySeedTestData.MarkerMessageId);
        first.ExitCode.Should().Be(expected: 0);
        hasHistoryBefore.Should().BeFalse();
        reset.ExitCode.Should().Be(expected: 0);
        hasHistoryAfter.Should().BeTrue();
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_OnACompleteModeTwoSeed_PrintsCompleteAndExitsWithZeroInBothModes()
    {
        var baseDirectory = TelemetryHistorySeedTestData.CreateBaseDirectory(TelemetryHistorySeedTestData.TestScript);
        await _fixture.RunJobAsync(
            [TelemetryHistorySeedTestData.TelemetryArgument],
            TestContext.Current.CancellationToken,
            baseDirectory,
            anchor: TelemetryHistorySeedTestData.ThursdayAnchor);

        var modeTwo = await _fixture.RunJobAsync(
            [TelemetryHistorySeedTestData.TelemetryArgument],
            TestContext.Current.CancellationToken,
            baseDirectory,
            anchor: TelemetryHistorySeedTestData.ThursdayAnchor);
        var modeOne = await _fixture.RunJobAsync(
            [],
            TestContext.Current.CancellationToken,
            baseDirectory,
            anchor: TelemetryHistorySeedTestData.ThursdayAnchor);

        modeTwo.ExitCode.Should().Be(expected: 0);
        modeTwo.Output.Should().Contain(SeedCompletionTestData.CompleteText);
        modeOne.ExitCode.Should().Be(expected: 0);
        modeOne.Output.Should().Contain(SeedCompletionTestData.CompleteText);
    }
}
