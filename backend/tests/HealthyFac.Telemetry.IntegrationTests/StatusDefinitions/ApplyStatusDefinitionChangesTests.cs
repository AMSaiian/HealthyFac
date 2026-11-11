using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.MetricDefinitions.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.IntegrationTests.StatusDefinitions;

[Collection(TelemetryDefinitionsCollection.Name)]
public sealed class ApplyStatusDefinitionChangesTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Apply_WithNewAndExistingCodes_CreatesTheNewOnesAndChangesTheExistingOneWithoutASchemaRevision()
    {
        var (tenant, machineId) = await ArrangeAsync();
        var existingId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            StatusDefinitionTestData.Code,
            StatusDefinitionTestData.Description,
            StatusSeverity.Info);

        var response = await _fixture.ApplyStatusChangesAsync(
            tenant.CompanyId,
            machineId,
            [
                StatusDefinitionTestData.Change(StatusDefinitionTestData.OtherCode),
                StatusDefinitionTestData.Change(
                    StatusDefinitionTestData.CodeOtherCase,
                    StatusSeverity.Error,
                    StatusDefinitionTestData.UpdatedDescription),
                StatusDefinitionTestData.Change(StatusDefinitionTestData.ThirdCode),
            ]);
        var updated = await _fixture.LoadStatusDefinitionRowAsync(existingId);
        var revision = await _fixture.LoadMetricSchemaRevisionAsync(machineId);

        response.Items.Select(item => (item.Code, item.IsCreated)).Should().Equal(
            (StatusDefinitionTestData.OtherCode, true),
            (StatusDefinitionTestData.Code, false),
            (StatusDefinitionTestData.ThirdCode, true));
        response.Items[index: 1].Id.Should().Be(existingId);

        updated.Code.Should().Be(StatusDefinitionTestData.Code);
        updated.Severity.Should().Be(StatusSeverity.Error);
        updated.Description.Should().Be(StatusDefinitionTestData.UpdatedDescription);
        revision.Should().Be(expected: 0);
    }

    [Fact]
    public async Task Apply_WithNullSeverityAndDescriptionForAnExistingCode_KeepsTheStoredValues()
    {
        var (tenant, machineId) = await ArrangeAsync();
        var existingId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            StatusDefinitionTestData.Code,
            StatusDefinitionTestData.Description,
            StatusSeverity.Error);

        await _fixture.ApplyStatusChangesAsync(
            tenant.CompanyId,
            machineId,
            [StatusDefinitionTestData.NoProposal(StatusDefinitionTestData.Code)]);
        var row = await _fixture.LoadStatusDefinitionRowAsync(existingId);

        row.Severity.Should().Be(StatusSeverity.Error);
        row.Description.Should().Be(StatusDefinitionTestData.Description);
    }

    [Fact]
    public async Task Apply_WithNullSeverityForANewCode_CreatesItAsInfo()
    {
        var (tenant, machineId) = await ArrangeAsync();

        var response = await _fixture.ApplyStatusChangesAsync(
            tenant.CompanyId,
            machineId,
            [StatusDefinitionTestData.NoProposal(StatusDefinitionTestData.Code)]);
        var row = await _fixture.LoadStatusDefinitionRowAsync(response.Items.Single().Id);

        row.Severity.Should().Be(StatusSeverity.Info);
        row.Description.Should().BeNull();
    }

    [Fact]
    public async Task Apply_WithOneInvalidItem_ChangesNothingAndNamesTheItemPath()
    {
        var (tenant, machineId) = await ArrangeAsync();

        var act = () => _fixture.ApplyStatusChangesAsync(
            tenant.CompanyId,
            machineId,
            [
                StatusDefinitionTestData.Change(StatusDefinitionTestData.Code),
                StatusDefinitionTestData.Change(code: string.Empty),
                StatusDefinitionTestData.Change(StatusDefinitionTestData.OtherCode),
            ]);

        var exception = (await act.Should().ThrowAsync<AppValidationException>()).Which;
        var firstRows = await _fixture.LoadStatusDefinitionRowsAsync(machineId, StatusDefinitionTestData.Code);
        var thirdRows = await _fixture.LoadStatusDefinitionRowsAsync(machineId, StatusDefinitionTestData.OtherCode);

        exception.Errors.Select(error => error.PropertyName).Should().Contain("items[1].code");
        firstRows.Should().BeEmpty();
        thirdRows.Should().BeEmpty();
    }

    [Fact]
    public async Task Apply_WithACodeRepeatedInOtherCase_IsAValidationFailureAtTheSecondItem()
    {
        var (tenant, machineId) = await ArrangeAsync();

        var act = () => _fixture.ApplyStatusChangesAsync(
            tenant.CompanyId,
            machineId,
            [
                StatusDefinitionTestData.Change(StatusDefinitionTestData.Code),
                StatusDefinitionTestData.Change(StatusDefinitionTestData.CodeOtherCase),
            ]);

        var exception = (await act.Should().ThrowAsync<AppValidationException>()).Which;
        exception.Errors.Select(error => error.PropertyName).Should().Contain("items[1].code");
    }

    [Fact]
    public async Task Apply_WithTheMaximumNumberOfItems_StoresAllOfThem()
    {
        var (tenant, machineId) = await ArrangeAsync();

        var response = await _fixture.ApplyStatusChangesAsync(
            tenant.CompanyId,
            machineId,
            StatusDefinitionTestData.Changes(StatusDefinitionValidationConstants.ApplyMaxItems));

        response.Items.Should().HaveCount(StatusDefinitionValidationConstants.ApplyMaxItems);
        response.Items.Should().OnlyContain(item => item.IsCreated);
    }

    [Fact]
    public async Task Apply_WithOneItemAboveTheMaximum_IsAValidationFailure()
    {
        var (tenant, machineId) = await ArrangeAsync();

        var act = () => _fixture.ApplyStatusChangesAsync(
            tenant.CompanyId,
            machineId,
            StatusDefinitionTestData.Changes(StatusDefinitionValidationConstants.ApplyMaxItems + 1));

        await act.Should().ThrowAsync<AppValidationException>();
    }

    [Fact]
    public async Task Apply_InParallelWithTheSameNewCode_OneSucceedsOneConflicts()
    {
        var (tenant, machineId) = await ArrangeAsync();
        using var race = _fixture.SaveGate.StartRace(StatusDefinitionTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.ApplyStatusChangesAsync(
                tenant.CompanyId,
                machineId,
                [StatusDefinitionTestData.Change(StatusDefinitionTestData.Code)])),
            race.RunAsync(() => _fixture.ApplyStatusChangesAsync(
                tenant.CompanyId,
                machineId,
                [StatusDefinitionTestData.Change(StatusDefinitionTestData.Code, StatusSeverity.Error)])));
        var rows = await _fixture.LoadStatusDefinitionRowsAsync(machineId, StatusDefinitionTestData.Code);

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null).Should().BeOfType<ConflictException>();
        rows.Should().ContainSingle();
    }

    private async Task<(MachineTenant Tenant, Guid MachineId)> ArrangeAsync()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);

        return (tenant, machineId);
    }
}
