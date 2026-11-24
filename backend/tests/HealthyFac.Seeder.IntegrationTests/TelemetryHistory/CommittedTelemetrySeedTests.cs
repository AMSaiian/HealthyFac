using System.Diagnostics;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.TelemetryHistory.Data;
using HealthyFac.Seeder.IntegrationTests.Timeline.Data;
using Npgsql;

namespace HealthyFac.Seeder.IntegrationTests.TelemetryHistory;

[Collection(SeederCollection.Name)]
public sealed class CommittedTelemetrySeedTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact(Explicit = true)]
    public async Task RunAsync_OnTheCommittedSeedInModeTwo_StoresTheHistoryOfTheFortyEightHours()
    {
        var stopwatch = Stopwatch.StartNew();
        var job = await _fixture.RunJobAsync(
            [TelemetryHistorySeedTestData.TelemetryArgument],
            TestContext.Current.CancellationToken,
            anchor: TelemetryHistorySeedTestData.ThursdayAnchor);
        stopwatch.Stop();

        var machineIds = await _fixture.LoadMachineIdsByKeyAsync();
        var counts = await _fixture.CountMessagesByMachineAsync();
        var range = await _fixture.FindSampleRangeAsync();
        var episodes = await _fixture.LoadEpisodesAsync();
        var (cases, signals) = await _fixture.LoadCasesAsync();
        var hasMarker = await _fixture.HasReceivedMessageAsync(SeedCompletionTestData.MarkerMessageId);
        var size = await ReadDatabaseSizeAsync();
        var step = await _fixture.ReadEvaluationStepAsync();

        TestContext.Current.SendDiagnosticMessage(
            $"Measured: elapsed {stopwatch.Elapsed:g}, "
            + $"telemetry database {size / TelemetryHistorySeedTestData.BytesPerMegabyte:F0} MB");
        job.ExitCode.Should().Be(expected: 0);
        hasMarker.Should().BeTrue();
        HistoryAssertions.AssertSamples(TelemetryHistorySeedTestData.CommittedExpectation, machineIds, counts, range);
        HistoryAssertions.AssertEpisodes(TelemetryHistorySeedTestData.CommittedExpectation, machineIds, episodes, step);
        HistoryAssertions.AssertCases(TelemetryHistorySeedTestData.CommittedExpectation, machineIds, cases, signals);
    }

    private async Task<long> ReadDatabaseSizeAsync()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_fixture.ConnectionStringOf(service: "telemetry"))
        {
            Pooling = false,
        }.ConnectionString;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(TelemetryHistorySeedTestData.DatabaseSizeSql, connection);

        return (long)(await command.ExecuteScalarAsync())!;
    }
}
