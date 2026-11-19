using EntityFramework.Exceptions.Common;
using HealthyFac.Telemetry.Infrastructure.OperatingPhases.Persistence;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.OperatingPhases.Data;

namespace HealthyFac.Telemetry.IntegrationTests.OperatingPhases;

[Collection(TelemetryPhasesCollection.Name)]
public sealed class OperatingPhaseConstraintTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Save_WithConditionsAndLimits_ReadsThemBack()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();

        var phaseId = await _fixture.AddPhaseAsync(arrangement, OperatingPhaseTestData.CuttingCode, priority: 0);

        var phase = await _fixture.LoadPhaseAsync(phaseId);

        phase.Conditions.Should().ContainSingle().Which.StatusDefinitionId.Should().Be(arrangement.StatusId);
        phase.Limits.Should().ContainSingle().Which.Limits.Should().Be(OperatingPhaseTestData.PhaseLimits);
    }

    [Fact]
    public async Task Save_WithDuplicateCodeOfActivePhases_ThrowsUniqueConstraint()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        await _fixture.AddPhaseAsync(arrangement, OperatingPhaseTestData.CuttingCode, priority: 0);

        var act = () => _fixture.AddPhaseAsync(arrangement, OperatingPhaseTestData.CuttingCode, priority: 1);

        var exception = await act.Should().ThrowAsync<UniqueConstraintException>();

        exception.Which.ConstraintName.Should().Be(OperatingPhaseIndexNames.MachineCode);
    }

    [Fact]
    public async Task Save_WithDuplicatePriorityOfActivePhases_ThrowsUniqueConstraint()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        await _fixture.AddPhaseAsync(arrangement, OperatingPhaseTestData.CuttingCode, priority: 0);

        var act = () => _fixture.AddPhaseAsync(arrangement, OperatingPhaseTestData.StoppedCode, priority: 0);

        var exception = await act.Should().ThrowAsync<UniqueConstraintException>();

        exception.Which.ConstraintName.Should().Be(OperatingPhaseIndexNames.MachinePriority);
    }

    [Fact]
    public async Task Save_WithCodeAndPriorityOfDeletedPhase_Succeeds()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var firstId = await _fixture.AddPhaseAsync(arrangement, OperatingPhaseTestData.CuttingCode, priority: 0);
        await _fixture.DeletePhaseAsync(firstId);

        var secondId = await _fixture.AddPhaseAsync(arrangement, OperatingPhaseTestData.CuttingCode, priority: 0);

        var first = await _fixture.LoadPhaseAsync(firstId);
        var second = await _fixture.LoadPhaseAsync(secondId);

        first.DeletedAt.Should().NotBeNull();
        second.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Insert_WithBothSourcesInACondition_IsRejectedByTheCheckConstraint()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var phaseId = await _fixture.AddPhaseAsync(arrangement, OperatingPhaseTestData.CuttingCode, priority: 0);

        var act = () => _fixture.ExecuteSqlAsync(
            OperatingPhaseTestData.InsertConditionWithBothSources(phaseId, arrangement.MetricId, arrangement.StatusId));

        await act.Should()
            .ThrowAsync<Exception>()
            .Where(exception => exception.ToString().Contains(OperatingPhaseCheckNames.ConditionExactlyOneSource));
    }

    [Fact]
    public async Task Insert_WithNoSourceInACondition_IsRejectedByTheCheckConstraint()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var phaseId = await _fixture.AddPhaseAsync(arrangement, OperatingPhaseTestData.CuttingCode, priority: 0);

        var act = () => _fixture.ExecuteSqlAsync(OperatingPhaseTestData.InsertConditionWithNoSource(phaseId));

        await act.Should()
            .ThrowAsync<Exception>()
            .Where(exception => exception.ToString().Contains(OperatingPhaseCheckNames.ConditionExactlyOneSource));
    }
}
