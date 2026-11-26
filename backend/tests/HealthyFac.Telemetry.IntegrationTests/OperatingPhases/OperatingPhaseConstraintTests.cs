using EntityFramework.Exceptions.Common;
using HealthyFac.Telemetry.Infrastructure.OperatingPhases.Persistence;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.OperatingPhases.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.IntegrationTests.OperatingPhases;

[Collection(TelemetryPhasesCollection.Name)]
[Feature(TestFeatures.SignalModel)]
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

    [Theory]
    [MemberData(
        nameof(OperatingPhaseTestData.DuplicatePhases), MemberType = typeof(OperatingPhaseTestData))]
    public async Task Save_WithDuplicateOfActivePhases_ThrowsUniqueConstraint(
        string caseName,
        string secondCode,
        int secondPriority,
        string expectedIndexName
    )
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        await _fixture.AddPhaseAsync(arrangement, OperatingPhaseTestData.CuttingCode, priority: 0);

        var act = () => _fixture.AddPhaseAsync(arrangement, secondCode, secondPriority);

        var exception = await act.Should().ThrowAsync<UniqueConstraintException>(caseName);

        exception.Which.ConstraintName.Should().Be(expectedIndexName);
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
