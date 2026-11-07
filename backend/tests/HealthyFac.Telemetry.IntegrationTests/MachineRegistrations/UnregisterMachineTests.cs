using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

namespace HealthyFac.Telemetry.IntegrationTests.MachineRegistrations;

public sealed class UnregisterMachineTests(TelemetryDatabaseFixture fixture) : IClassFixture<TelemetryDatabaseFixture>
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task UnregisterMachine_WithRegistration_SoftDeletes()
    {
        var message = MachineRegistrationTestData.CreateMessage();
        await _fixture.RegisterMachineAsync(message);

        await _fixture.UnregisterMachineAsync(MachineRegistrationTestData.CreateDeletedMessage(message.MachineId));

        var registration = await _fixture.LoadRegistrationAsync(message.MachineId);

        registration!.DeletedAt.Should().NotBeNull();
        registration.DeletedBy.Should().NotBeNull();
    }

    [Fact]
    public async Task UnregisterMachine_WithStatusDefinitions_SoftDeletesThem()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var otherMachineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var statusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId, tenant.BothDepartmentIds, machineId, StatusDefinitionTestData.Code);
        var otherStatusId = await _fixture.CreateStatusDefinitionAsync(
            tenant.CompanyId, tenant.BothDepartmentIds, otherMachineId, StatusDefinitionTestData.Code);

        await _fixture.UnregisterMachineAsync(
            new MachineDeleted { MachineId = machineId, CompanyId = tenant.CompanyId });

        var status = await _fixture.LoadStatusDefinitionRowAsync(statusId);
        var otherStatus = await _fixture.LoadStatusDefinitionRowAsync(otherStatusId);

        status.DeletedAt.Should().NotBeNull();
        status.DeletedBy.Should().NotBeNull();
        otherStatus.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task UnregisterMachine_WithMetricDefinitions_SoftDeletesThem()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var otherMachineId = await _fixture.CreateStatusDefinitionMachineAsync(tenant);
        var metricId = await _fixture.AddMetricDefinitionAsync(machineId);
        var otherMetricId = await _fixture.AddMetricDefinitionAsync(otherMachineId);

        await _fixture.UnregisterMachineAsync(
            new MachineDeleted { MachineId = machineId, CompanyId = tenant.CompanyId });

        var metric = await _fixture.LoadMetricDefinitionRowAsync(metricId);
        var otherMetric = await _fixture.LoadMetricDefinitionRowAsync(otherMetricId);

        metric.DeletedAt.Should().NotBeNull();
        metric.DeletedBy.Should().NotBeNull();
        otherMetric.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task UnregisterMachine_WithoutRegistration_Throws()
    {
        var message = MachineRegistrationTestData.CreateDeletedMessage(Guid.CreateVersion7());

        var act = () => _fixture.UnregisterMachineAsync(message);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.RegistrationNotFound);
    }

    [Fact]
    public async Task UnregisterMachine_WithDeletedRegistration_Succeeds()
    {
        var message = MachineRegistrationTestData.CreateMessage();
        await _fixture.RegisterMachineAsync(message);
        var deleted = MachineRegistrationTestData.CreateDeletedMessage(message.MachineId);
        await _fixture.UnregisterMachineAsync(deleted);

        var act = () => _fixture.UnregisterMachineAsync(deleted);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RegisterMachine_AfterUnregister_ChangesNothing()
    {
        var message = MachineRegistrationTestData.CreateMessage();
        await _fixture.RegisterMachineAsync(message);
        await _fixture.UnregisterMachineAsync(MachineRegistrationTestData.CreateDeletedMessage(message.MachineId));

        await _fixture.RegisterMachineAsync(message);

        var registration = await _fixture.LoadRegistrationAsync(message.MachineId);

        registration!.DeletedAt.Should().NotBeNull();
    }
}
