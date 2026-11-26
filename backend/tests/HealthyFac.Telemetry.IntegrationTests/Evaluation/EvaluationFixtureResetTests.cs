using HealthyFac.Telemetry.Application.Evaluation;
using HealthyFac.Telemetry.Infrastructure.Evaluation;
using HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.IntegrationTests.Evaluation;

[Collection(TelemetryEvaluationCollection.Name)]
[Feature(TestFeatures.Evaluation)]
public sealed class EvaluationFixtureResetTests(TelemetryEvaluationFixture fixture)
{
    private readonly TelemetryEvaluationFixture _fixture = fixture;

    [Fact]
    public async Task ResetAsync_AfterAWorkerRoundFoundTheFleetSilent_LeavesTheFleetNotSilent()
    {
        await _fixture.Database.GetHostedService<EvaluationWorker>().RunOnceAsync(CancellationToken.None);
        var wasSilent = _fixture.Database.GetSingleton<FleetSilence>().IsSilent;

        await _fixture.ResetAsync();

        var isSilent = _fixture.Database.GetSingleton<FleetSilence>().IsSilent;

        wasSilent.Should().BeTrue();
        isSilent.Should().BeFalse();
    }
}
