using System.Diagnostics.Metrics;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Telemetry.Infrastructure.Evaluation;
using HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace HealthyFac.Telemetry.IntegrationTests.Evaluation;

[Collection(TelemetryEvaluationCollection.Name)]
public sealed class EvaluationWorkerMetricsTests(TelemetryEvaluationFixture fixture)
{
    private const int DueMachineCount = 3;

    private readonly TelemetryEvaluationFixture _fixture = fixture;

    [Fact]
    public async Task RunOnceAsync_WithACompletedRun_CountsItWithTheResultAndRecordsOneDuration()
    {
        await _fixture.Database.RegisterTenantMachineAsync(MachineRegistrationTestData.CompanyId);
        using var runs = Collect<long>(ObservabilityConstants.Evaluation.Instruments.Runs);
        using var durations = Collect<double>(ObservabilityConstants.Evaluation.Instruments.RunDuration);
        _fixture.Time.SetUtcNow(_fixture.At(seconds: 1));

        await _fixture.Database.GetHostedService<EvaluationWorker>().RunOnceAsync(CancellationToken.None);

        runs.GetMeasurementSnapshot()
            .Should()
            .ContainSingle()
            .Which.Should()
            .Match<CollectedMeasurement<long>>(measurement =>
                measurement.Value == 1 && HasResult(measurement.Tags, "completed"));

        durations.GetMeasurementSnapshot()
            .Should()
            .ContainSingle()
            .Which.Value.Should()
            .BeGreaterThanOrEqualTo(expected: 0);
    }

    [Fact]
    public async Task RunOnceAsync_WithAFailedRun_CountsItAsFailed()
    {
        await _fixture.Database.RegisterTenantMachineAsync(MachineRegistrationTestData.CompanyId);
        using var runs = Collect<long>(ObservabilityConstants.Evaluation.Instruments.Runs);
        _fixture.Time.SetUtcNow(_fixture.At(seconds: 1));
        _fixture.RunFailure.Arm();

        await _fixture.Database.GetHostedService<EvaluationWorker>().RunOnceAsync(CancellationToken.None);

        runs.GetMeasurementSnapshot()
            .Should()
            .ContainSingle()
            .Which.Should()
            .Match<CollectedMeasurement<long>>(measurement =>
                measurement.Value == 1 && HasResult(measurement.Tags, "failed"));
    }

    [Fact]
    public async Task RunOnceAsync_WithACancelledRun_RecordsNothing()
    {
        await _fixture.Database.RegisterTenantMachineAsync(MachineRegistrationTestData.CompanyId);
        using var runs = Collect<long>(ObservabilityConstants.Evaluation.Instruments.Runs);
        using var durations = Collect<double>(ObservabilityConstants.Evaluation.Instruments.RunDuration);
        using var cancellation = new CancellationTokenSource();
        _fixture.Time.SetUtcNow(_fixture.At(seconds: 1));
        _fixture.RunFailure.Arm(new OperationCanceledException(cancellation.Token), cancellation.Cancel);

        var act = () => _fixture.Database.GetHostedService<EvaluationWorker>().RunOnceAsync(cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        runs.GetMeasurementSnapshot().Should().BeEmpty();
        durations.GetMeasurementSnapshot().Should().BeEmpty();
    }

    [Fact]
    public async Task RunOnceAsync_WithADueMachine_ReportsTheCountOfDueMachinesInTheGauge()
    {
        for (var index = 0; index < DueMachineCount; index++)
        {
            await _fixture.Database.RegisterTenantMachineAsync(MachineRegistrationTestData.CompanyId);
        }

        using var gauge = Collect<int>(ObservabilityConstants.Evaluation.Instruments.DueMachines);
        _fixture.Time.SetUtcNow(_fixture.At(seconds: 1));

        await _fixture.Database.GetHostedService<EvaluationWorker>().RunOnceAsync(CancellationToken.None);
        gauge.RecordObservableInstruments();

        gauge.LastMeasurement!.Value.Should().Be(DueMachineCount);
    }

    private MetricCollector<T> Collect<T>(string instrument)
        where T : struct =>
        new(
            _fixture.Database.GetSingleton<IMeterFactory>(),
            ObservabilityConstants.Evaluation.Meter,
            instrument);

    private static bool HasResult(IReadOnlyDictionary<string, object?> tags, string result) =>
        tags.TryGetValue(ObservabilityConstants.Evaluation.Tags.Result, out var value) && Equals(value, result);
}
