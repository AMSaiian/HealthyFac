using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.Ingest.Data;
using HealthyFac.Seeder.IntegrationTests.Relay.Data;
using HealthyFac.Seeder.IntegrationTests.Timeline.Data;
using HealthyFac.Seeder.IntegrationTests.WorkOrders.Data;

namespace HealthyFac.Seeder.IntegrationTests.Timeline;

[Collection(SeederCollection.Name)]
public sealed class SeedCompletionTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact(Explicit = true)]
    public async Task RunAsync_SecondRunAfterAFullSeed_ExitsWithZeroAndChangesNothing()
    {
        var first = await _fixture.RunJobAsync([], TestContext.Current.CancellationToken);
        var before = await _fixture.TakeSnapshotAsync();

        var second = await _fixture.RunJobAsync([], TestContext.Current.CancellationToken);

        var after = await _fixture.TakeSnapshotAsync();
        first.ExitCode.Should().Be(expected: 0);
        second.ExitCode.Should().Be(expected: 0);
        second.Output.Should().Contain(SeedCompletionTestData.CompleteText);
        after.Should().BeEquivalentTo(before);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithTelemetryOnAModeOneSeed_ReportsTheMissingHistoryAndWritesNothing()
    {
        await _fixture.RunJobAsync([], TestContext.Current.CancellationToken);
        var before = await _fixture.TakeSnapshotAsync();

        var next = await _fixture.RunJobAsync(
            [SeedCompletionTestData.TelemetryArgument], TestContext.Current.CancellationToken);

        var after = await _fixture.TakeSnapshotAsync();
        next.ExitCode.Should().Be(expected: 1);

        next.Output.Should()
            .Contain(SeedCompletionTestData.NoCompleteHistoryText)
            .And.Contain(SeedCompletionTestData.ResetTelemetryHint);

        after.Should().BeEquivalentTo(before);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithTheMarkerMessage_ReportsTheSeedCompleteInBothModes()
    {
        await _fixture.RunJobAsync([], TestContext.Current.CancellationToken);
        await _fixture.AddReceivedMessageAsync(SeedCompletionTestData.MarkerMessageId);

        var modeOne = await _fixture.RunJobAsync([], TestContext.Current.CancellationToken);

        var modeTwo = await _fixture.RunJobAsync(
            [SeedCompletionTestData.TelemetryArgument], TestContext.Current.CancellationToken);

        modeOne.ExitCode.Should().Be(expected: 0);
        modeOne.Output.Should().Contain(SeedCompletionTestData.CompleteText);
        modeTwo.ExitCode.Should().Be(expected: 0);
        modeTwo.Output.Should().Contain(SeedCompletionTestData.CompleteText);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithReceivedMessagesOfALiveMachine_KeepsTheModeOneSeedCompleteAndFlagsTheMissingHistory()
    {
        await _fixture.RunJobAsync([], TestContext.Current.CancellationToken);
        await _fixture.AddReceivedMessageAsync(SeedCompletionTestData.OtherMessageId);
        await _fixture.AddReceivedMessageAsync(Guid.CreateVersion7());
        await _fixture.AddReceivedMessageAsync(Guid.CreateVersion7());

        var modeOne = await _fixture.RunJobAsync([], TestContext.Current.CancellationToken);

        var modeTwo = await _fixture.RunJobAsync(
            [SeedCompletionTestData.TelemetryArgument], TestContext.Current.CancellationToken);

        modeOne.ExitCode.Should().Be(expected: 0);
        modeOne.Output.Should().Contain(SeedCompletionTestData.CompleteText);
        modeTwo.ExitCode.Should().Be(expected: 1);

        modeTwo.Output.Should()
            .Contain(SeedCompletionTestData.NoCompleteHistoryText)
            .And.Contain(SeedCompletionTestData.ResetTelemetryHint);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_AfterAFailedRun_ReportsAnIncompleteSeedAndWritesNothing()
    {
        var baseDirectory = SeedBaseDirectory.Create(WorkOrderSeedVariants.PumpCouplingSlotTaken());

        var failed = await _fixture.RunJobAsync(
            [], TestContext.Current.CancellationToken, baseDirectory);

        var before = await _fixture.TakeSnapshotAsync();

        var next = await _fixture.RunJobAsync([], TestContext.Current.CancellationToken);

        var after = await _fixture.TakeSnapshotAsync();
        failed.ExitCode.Should().Be(expected: 1);
        next.ExitCode.Should().Be(expected: 1);

        next.Output.Should()
            .Contain(SeedCompletionTestData.IncompleteText)
            .And.Contain(SeedCompletionTestData.ResetHint);

        after.Should().BeEquivalentTo(before);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_AfterAStopAtTheEndOfThePeoplePart_LeavesNoHalfWrittenUseCaseAndReportsAnIncompleteSeed()
    {
        using var stop = new CancellationTokenSource();
        var log = new CancellingWriter(SeedCompletionTestData.FirstIngestItem, stop);
        var stopped = await _fixture.RunJobAsync([], stop.Token, output: log);
        var before = await _fixture.TakeSnapshotAsync();
        var openCount = await _fixture.CountOpenMessagesAsync();
        var files = await _fixture.LoadFileRefsAsync();

        var next = await _fixture.RunJobAsync([], TestContext.Current.CancellationToken);

        var after = await _fixture.TakeSnapshotAsync();
        stopped.ExitCode.Should().Be(expected: 1);
        stopped.Output.Should().Contain(SeedCompletionTestData.StoppedText);
        openCount.Should().Be(expected: 0);
        files.Should().BeEmpty();
        next.ExitCode.Should().Be(expected: 1);
        next.Output.Should().Contain(SeedCompletionTestData.IncompleteText);
        after.Should().BeEquivalentTo(before);
    }
}
