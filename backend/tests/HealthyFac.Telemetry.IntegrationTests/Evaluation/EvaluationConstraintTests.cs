using EntityFramework.Exceptions.Common;
using HealthyFac.Telemetry.Domain.SignalEpisodes;
using HealthyFac.Telemetry.Infrastructure.Evaluation.Persistence;
using HealthyFac.Telemetry.Infrastructure.SignalEpisodes.Persistence;
using HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;
using HealthyFac.Telemetry.IntegrationTests.OperatingPhases.Data;

namespace HealthyFac.Telemetry.IntegrationTests.Evaluation;

[Collection(TelemetryEvaluationCollection.Name)]
public sealed class EvaluationConstraintTests(TelemetryEvaluationFixture fixture)
{
    private readonly TelemetryEvaluationFixture _fixture = fixture;

    [Fact]
    public async Task Save_WithASecondOpenEpisodeOfTheSameMachineRuleAndMetric_ThrowsUniqueConstraint()
    {
        var arrangement = await _fixture.Database.ArrangePhaseMachineAsync();
        await _fixture.AddEpisodeAsync(arrangement, SignalRule.Threshold, arrangement.MetricId, statusId: null);

        var act = () => _fixture.AddEpisodeAsync(
            arrangement, SignalRule.Threshold, arrangement.MetricId, statusId: null);

        var exception = await act.Should().ThrowAsync<UniqueConstraintException>();

        exception.Which.ConstraintName.Should().Be(SignalEpisodeIndexNames.OpenSubject);
    }

    [Fact]
    public async Task Save_WithASecondOpenEpisodeWithoutASubject_ThrowsUniqueConstraint()
    {
        var arrangement = await _fixture.Database.ArrangePhaseMachineAsync();
        await _fixture.AddEpisodeAsync(arrangement, SignalRule.SilentMachine, metricId: null, statusId: null);

        var act = () => _fixture.AddEpisodeAsync(
            arrangement, SignalRule.SilentMachine, metricId: null, statusId: null);

        var exception = await act.Should().ThrowAsync<UniqueConstraintException>();

        exception.Which.ConstraintName.Should().Be(SignalEpisodeIndexNames.OpenSubject);
    }

    [Fact]
    public async Task Save_WithAnOpenEpisodeOfAnotherSubject_Succeeds()
    {
        var arrangement = await _fixture.Database.ArrangePhaseMachineAsync();
        await _fixture.AddEpisodeAsync(arrangement, SignalRule.Threshold, arrangement.MetricId, statusId: null);

        var otherId = await _fixture.AddResolvableEpisodeAsync(
            arrangement, SignalRule.Status, metricId: null, arrangement.StatusId);

        otherId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Save_WithAnOpenEpisodeAfterTheFirstResolved_Succeeds()
    {
        var arrangement = await _fixture.Database.ArrangePhaseMachineAsync();

        var firstId = await _fixture.AddResolvableEpisodeAsync(
            arrangement, SignalRule.SilentMachine, metricId: null, statusId: null);

        await _fixture.ResolveEpisodeAsync(firstId);

        var secondId = await _fixture.AddResolvableEpisodeAsync(
            arrangement, SignalRule.SilentMachine, metricId: null, statusId: null);

        secondId.Should().NotBe(firstId);
    }

    [Fact]
    public async Task Save_WithASecondStatisticsRowOfThePhaseNone_ThrowsUniqueConstraint()
    {
        var arrangement = await _fixture.Database.ArrangePhaseMachineAsync();
        await _fixture.AddStatisticsAsync(arrangement.MetricId, phaseId: null);

        var act = () => _fixture.AddStatisticsAsync(arrangement.MetricId, phaseId: null);

        var exception = await act.Should().ThrowAsync<UniqueConstraintException>();

        exception.Which.ConstraintName.Should().Be(EvaluationIndexNames.MetricPhase);
    }
}
