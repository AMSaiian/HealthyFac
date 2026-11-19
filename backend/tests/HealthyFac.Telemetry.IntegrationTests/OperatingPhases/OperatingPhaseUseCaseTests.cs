using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.OperatingPhases.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.IntegrationTests.OperatingPhases;

[Collection(TelemetryPhasesCollection.Name)]
public sealed class OperatingPhaseUseCaseTests(TelemetryDatabaseFixture fixture)
{
    private static readonly Guid CompanyId = MachineRegistrationTestData.CompanyId;

    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task CreatePhase_WithConditionsAndLimits_GetReturnsThem()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var request = OperatingPhaseTestData.CreateRequest(arrangement) with
        {
            Conditions =
            [
                OperatingPhaseTestData.StatusCondition(arrangement.StatusId),
                new PhaseConditionInput
                {
                    MetricDefinitionId = arrangement.StateMetricId,
                    Operator = PhaseConditionOperator.In,
                    States = ["running"],
                },
            ],
            Limits =
            [
                new PhaseLimitInput { MetricDefinitionId = arrangement.MetricId, WarningHigh = 80, AlarmHigh = 90 },
            ],
        };

        var phaseId = await _fixture.CreatePhaseUseCaseAsync(request);

        var phase = await _fixture.GetPhaseUseCaseAsync(CompanyId, arrangement.MachineId, phaseId);

        phase.Code.Should().Be(request.Code);
        phase.IsTracked.Should().BeTrue();
        phase.Conditions.Should().HaveCount(expected: 2);
        phase.Conditions.Single(condition => condition.StatusDefinitionId is not null)
            .Operator.Should()
            .Be(PhaseConditionOperator.Active);
        phase.Conditions.Single(condition => condition.MetricDefinitionId == arrangement.StateMetricId)
            .States.Should()
            .Equal("running");
        phase.Limits.Should()
            .ContainSingle()
            .Which.AlarmHigh.Should()
            .Be(expected: 90);
    }

    [Fact]
    public async Task ListPhases_WithSeveralPhases_ReturnsThemInPriorityOrder()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        await _fixture.CreatePhaseUseCaseAsync(
            OperatingPhaseTestData.CreateRequest(arrangement, OperatingPhaseTestData.StoppedCode, priority: 1));
        await _fixture.CreatePhaseUseCaseAsync(
            OperatingPhaseTestData.CreateRequest(arrangement, OperatingPhaseTestData.CuttingCode, priority: 0));

        var phases = await _fixture.ListPhasesUseCaseAsync(CompanyId, arrangement.MachineId);

        phases.Select(phase => phase.Code)
            .Should()
            .Equal(
                OperatingPhaseTestData.CuttingCode,
                OperatingPhaseTestData.StoppedCode);
    }

    [Fact]
    public async Task UpdatePhase_WithNewBody_ReplacesCodeConditionsAndLimits()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var phaseId = await _fixture.CreatePhaseUseCaseAsync(
            OperatingPhaseTestData.CreateRequest(arrangement) with
            {
                Limits = [new PhaseLimitInput { MetricDefinitionId = arrangement.MetricId, AlarmHigh = 90 }],
            });

        await _fixture.UpdatePhaseUseCaseAsync(
            new UpdateOperatingPhaseRequest
            {
                CompanyId = CompanyId,
                MachineId = arrangement.MachineId,
                Id = phaseId,
                Code = OperatingPhaseTestData.StoppedCode,
                Name = "Stopped",
                Priority = 3,
                IsTracked = false,
                Conditions =
                [
                    new PhaseConditionInput
                    {
                        MetricDefinitionId = arrangement.MetricId,
                        Operator = PhaseConditionOperator.GreaterThan,
                        Value = 10,
                    },
                ],
                Limits = [],
            });

        var phase = await _fixture.GetPhaseUseCaseAsync(CompanyId, arrangement.MachineId, phaseId);

        phase.Code.Should().Be(OperatingPhaseTestData.StoppedCode);
        phase.Priority.Should().Be(expected: 3);
        phase.IsTracked.Should().BeFalse();
        phase.Conditions.Should()
            .ContainSingle()
            .Which.MetricDefinitionId.Should()
            .Be(arrangement.MetricId);
        phase.Limits.Should().BeEmpty();
    }

    [Fact]
    public async Task DeletePhase_WithExistingPhase_HidesItFromGetAndList()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var phaseId = await _fixture.CreatePhaseUseCaseAsync(OperatingPhaseTestData.CreateRequest(arrangement));

        await _fixture.DeletePhaseUseCaseAsync(CompanyId, arrangement.MachineId, phaseId);

        var act = () => _fixture.GetPhaseUseCaseAsync(CompanyId, arrangement.MachineId, phaseId);
        var phases = await _fixture.ListPhasesUseCaseAsync(CompanyId, arrangement.MachineId);

        await act.Should().ThrowAsync<NotFoundException>();
        phases.Should().BeEmpty();
    }

    [Fact]
    public async Task DeletePhase_WithConditionsAndLimits_SoftDeletesTheirRows()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var phaseId = await _fixture.CreatePhaseUseCaseAsync(
            OperatingPhaseTestData.CreateRequest(arrangement) with
            {
                Limits = [new PhaseLimitInput { MetricDefinitionId = arrangement.MetricId, AlarmHigh = 90 }],
            });

        await _fixture.DeletePhaseUseCaseAsync(CompanyId, arrangement.MachineId, phaseId);

        var phase = await _fixture.LoadPhaseAsync(phaseId);
        var rows = await _fixture.LoadPhaseRowsAsync(phaseId);

        phase.DeletedAt.Should().NotBeNull();
        rows.Conditions.Should().ContainSingle().Which.DeletedAt.Should().NotBeNull();
        rows.Limits.Should().ContainSingle().Which.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdatePhase_DroppingAConditionAndALimitRow_SoftDeletesBothAndGetReturnsTheRest()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var phaseId = await _fixture.CreatePhaseUseCaseAsync(
            OperatingPhaseTestData.CreateRequest(arrangement) with
            {
                Conditions =
                [
                    OperatingPhaseTestData.StatusCondition(arrangement.StatusId),
                    new PhaseConditionInput
                    {
                        MetricDefinitionId = arrangement.StateMetricId,
                        Operator = PhaseConditionOperator.In,
                        States = [OperatingPhaseTestData.StateMetricStates[index: 1]],
                    },
                ],
                Limits = [new PhaseLimitInput { MetricDefinitionId = arrangement.MetricId, AlarmHigh = 90 }],
            });

        await UpdateAsync(arrangement, phaseId, [OperatingPhaseTestData.StatusCondition(arrangement.StatusId)], []);

        var phase = await _fixture.GetPhaseUseCaseAsync(CompanyId, arrangement.MachineId, phaseId);
        var rows = await _fixture.LoadPhaseRowsAsync(phaseId);

        phase.Conditions.Should().ContainSingle().Which.StatusDefinitionId.Should().Be(arrangement.StatusId);
        phase.Limits.Should().BeEmpty();
        rows.Conditions.Where(condition => condition.MetricDefinitionId == arrangement.StateMetricId)
            .Should()
            .ContainSingle()
            .Which.DeletedAt.Should()
            .NotBeNull();
        rows.Limits.Should().ContainSingle().Which.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdatePhase_KeepingTheMetricOfALimitRow_ChangesTheRowInPlace()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var phaseId = await _fixture.CreatePhaseUseCaseAsync(
            OperatingPhaseTestData.CreateRequest(arrangement) with
            {
                Limits = [new PhaseLimitInput { MetricDefinitionId = arrangement.MetricId, AlarmHigh = 90 }],
            });
        var before = await _fixture.LoadPhaseRowsAsync(phaseId);

        await UpdateAsync(
            arrangement,
            phaseId,
            [OperatingPhaseTestData.StatusCondition(arrangement.StatusId)],
            [new PhaseLimitInput { MetricDefinitionId = arrangement.MetricId, AlarmHigh = 95 }]);

        var after = await _fixture.LoadPhaseRowsAsync(phaseId);

        after.Limits.Should().ContainSingle();
        after.Limits[index: 0].Id.Should().Be(before.Limits[index: 0].Id);
        after.Limits[index: 0].AlarmHigh.Should().Be(expected: 95);
        after.Limits[index: 0].DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePhase_AddingALimitRowAfterItsOldRowWasSoftDeleted_Succeeds()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var phaseId = await _fixture.CreatePhaseUseCaseAsync(
            OperatingPhaseTestData.CreateRequest(arrangement) with
            {
                Limits = [new PhaseLimitInput { MetricDefinitionId = arrangement.MetricId, AlarmHigh = 90 }],
            });
        var conditions = new[] { OperatingPhaseTestData.StatusCondition(arrangement.StatusId) };
        await UpdateAsync(arrangement, phaseId, conditions, []);

        await UpdateAsync(
            arrangement,
            phaseId,
            conditions,
            [new PhaseLimitInput { MetricDefinitionId = arrangement.MetricId, AlarmHigh = 80 }]);

        var phase = await _fixture.GetPhaseUseCaseAsync(CompanyId, arrangement.MachineId, phaseId);
        var rows = await _fixture.LoadPhaseRowsAsync(phaseId);

        phase.Limits.Should().ContainSingle();
        rows.Limits.Should().HaveCount(expected: 2);
        rows.Limits.Count(limit => limit.DeletedAt is null).Should().Be(expected: 1);
    }

    [Fact]
    public async Task ReorderPhases_WithAllPhases_SetsPrioritiesFromZero()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var first = await _fixture.CreatePhaseUseCaseAsync(
            OperatingPhaseTestData.CreateRequest(arrangement, OperatingPhaseTestData.CuttingCode, priority: 0));
        var second = await _fixture.CreatePhaseUseCaseAsync(
            OperatingPhaseTestData.CreateRequest(arrangement, OperatingPhaseTestData.StoppedCode, priority: 1));
        var third = await _fixture.CreatePhaseUseCaseAsync(
            OperatingPhaseTestData.CreateRequest(arrangement, OperatingPhaseTestData.IdleCode, priority: 2));

        await _fixture.ReorderPhasesUseCaseAsync(CompanyId, arrangement.MachineId, [third, first, second]);

        var phases = await _fixture.ListPhasesUseCaseAsync(CompanyId, arrangement.MachineId);

        phases.Select(phase => (phase.Id, phase.Priority)).Should().Equal((third, 0), (first, 1), (second, 2));
    }

    [Fact]
    public async Task ReorderPhases_WithMissingPhase_ThrowsValidation()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var first = await _fixture.CreatePhaseUseCaseAsync(
            OperatingPhaseTestData.CreateRequest(arrangement, OperatingPhaseTestData.CuttingCode, priority: 0));
        await _fixture.CreatePhaseUseCaseAsync(
            OperatingPhaseTestData.CreateRequest(arrangement, OperatingPhaseTestData.StoppedCode, priority: 1));

        var act = () => _fixture.ReorderPhasesUseCaseAsync(CompanyId, arrangement.MachineId, [first]);

        var exception = await act.Should().ThrowAsync<AppValidationException>();

        exception.Which.Errors.Should()
            .ContainSingle()
            .Which.PropertyName.Should()
            .Be(OperatingPhaseTestData.PhaseIdsKey);
    }

    [Theory]
    [MemberData(nameof(OperatingPhaseTestData.InvalidBodies), MemberType = typeof(OperatingPhaseTestData))]
    public async Task CreatePhase_WithInvalidBody_ThrowsValidation(
        string caseName,
        Func<PhaseArrangement, CreateOperatingPhaseRequest> createRequest
    )
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();

        var act = () => _fixture.CreatePhaseUseCaseAsync(createRequest(arrangement));

        await act.Should().ThrowAsync<AppValidationException>(caseName);
    }

    [Fact]
    public async Task CreatePhase_WithTakenCode_ThrowsConflict()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        await _fixture.CreatePhaseUseCaseAsync(OperatingPhaseTestData.CreateRequest(arrangement, priority: 0));

        var act = () => _fixture.CreatePhaseUseCaseAsync(
            OperatingPhaseTestData.CreateRequest(arrangement, priority: 1));

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreatePhase_WithTakenPriority_ThrowsConflict()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        await _fixture.CreatePhaseUseCaseAsync(OperatingPhaseTestData.CreateRequest(arrangement, priority: 0));

        var act = () => _fixture.CreatePhaseUseCaseAsync(
            OperatingPhaseTestData.CreateRequest(arrangement, OperatingPhaseTestData.StoppedCode, priority: 0));

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task GetPhase_ForMachineOfOtherCompany_ThrowsNotFound()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var phaseId = await _fixture.CreatePhaseUseCaseAsync(OperatingPhaseTestData.CreateRequest(arrangement));

        var act = () => _fixture.GetPhaseUseCaseAsync(
            MachineRegistrationTestData.OtherCompanyId,
            arrangement.MachineId,
            phaseId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteStatusDefinition_UsedByPhaseCondition_ThrowsUnprocessableAndKeepsTheStatus()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        await _fixture.CreatePhaseUseCaseAsync(OperatingPhaseTestData.CreateRequest(arrangement));

        var act = () => _fixture.DeleteStatusDefinitionAsync(CompanyId, arrangement.MachineId, arrangement.StatusId);

        await act.Should().ThrowAsync<UnprocessableException>().WithMessage(StatusDefinitionErrors.UsedByPhase);
        var status = await _fixture.GetStatusDefinitionAsync(CompanyId, arrangement.MachineId, arrangement.StatusId);

        status.Code.Should().Be(OperatingPhaseTestData.StatusCode);
    }

    [Fact]
    public async Task DeleteStatusDefinition_UsedOnlyByDeletedPhase_Succeeds()
    {
        var arrangement = await _fixture.ArrangePhaseMachineAsync();
        var phaseId = await _fixture.CreatePhaseUseCaseAsync(OperatingPhaseTestData.CreateRequest(arrangement));
        await _fixture.DeletePhaseUseCaseAsync(CompanyId, arrangement.MachineId, phaseId);

        await _fixture.DeleteStatusDefinitionAsync(CompanyId, arrangement.MachineId, arrangement.StatusId);

        var act = () => _fixture.GetStatusDefinitionAsync(CompanyId, arrangement.MachineId, arrangement.StatusId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private Task UpdateAsync(
        PhaseArrangement arrangement,
        Guid phaseId,
        IReadOnlyList<PhaseConditionInput> conditions,
        IReadOnlyList<PhaseLimitInput> limits
    ) =>
        _fixture.UpdatePhaseUseCaseAsync(
            new UpdateOperatingPhaseRequest
            {
                CompanyId = CompanyId,
                MachineId = arrangement.MachineId,
                Id = phaseId,
                Code = OperatingPhaseTestData.CuttingCode,
                Name = OperatingPhaseTestData.CuttingCode,
                Priority = 0,
                IsTracked = true,
                Conditions = conditions,
                Limits = limits,
            });
}
