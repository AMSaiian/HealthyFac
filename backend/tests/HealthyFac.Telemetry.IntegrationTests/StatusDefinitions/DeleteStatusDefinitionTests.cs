using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.IntegrationTests.StatusDefinitions;

[Collection(TelemetryDefinitionsCollection.Name)]
public sealed class DeleteStatusDefinitionTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task DeleteStatusDefinition_Twice_ThrowsNotFound()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            StatusDefinitionTestData.Code);
        await _fixture.DeleteStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            statusId);

        var act = () => _fixture.DeleteStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            statusId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(StatusDefinitionErrors.StatusDefinitionNotFound);
    }

    [Fact]
    public async Task DeleteStatusDefinition_ForMachineOfOtherCompany_ThrowsNotFoundAndKeepsRowActive()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var otherTenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(otherTenant);
        var statusId = await _fixture.CreateStatusDefinitionAsync(
            otherTenant.CompanyId,
            machineId,
            StatusDefinitionTestData.Code);

        var act = () => _fixture.DeleteStatusDefinitionAsync(
            tenant.CompanyId,
            machineId,
            statusId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.MachineNotFound);
        var row = await _fixture.LoadStatusDefinitionRowAsync(statusId);

        row.DeletedAt.Should().BeNull();
    }
}
