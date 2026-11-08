using EntityFramework.Exceptions.Common;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Domain.Gateways;

namespace HealthyFac.Core.IntegrationTests.Machines;

public sealed class CreateMachineTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task CreateMachine_WithValidRequest_StoresMachineWithDescription()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            MachineTestData.BulletListDescription);

        var machine = await _fixture.LoadMachineRowAsync(machineId);

        machine.Name.Should().Be(MachineTestData.MachineName);
        machine.Description.Should().Be(MachineTestData.BulletListDescription);
        machine.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateMachine_WithValidRequest_StoresGatewayId()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);

        var machine = await _fixture.LoadMachineRowAsync(machineId);

        GatewayIds.IsValid(machine.GatewayId).Should().BeTrue();
    }

    [Fact]
    public async Task CreateMachine_WithTakenGatewayId_ThrowsUniqueConstraint()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var gatewayId = GatewayIds.Create();
        await _fixture.AddMachineWithGatewayIdAsync(
            tenant.CompanyId, tenant.FirstDepartmentId, gatewayId, MachineTestData.SerialNumber);

        var act = () => _fixture.AddMachineWithGatewayIdAsync(
            tenant.CompanyId, tenant.FirstDepartmentId, gatewayId, MachineTestData.OtherSerialNumber);

        await act.Should().ThrowAsync<UniqueConstraintException>();
    }

    [Fact]
    public async Task CreateMachine_WithValidRequest_StoresMachineCreatedOutboxRow()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);

        var machine = await _fixture.LoadMachineRowAsync(machineId);
        var messages = await _fixture.ReadOutboxMessagesAsync<MachineCreated>();
        var message = messages.Where(candidate => candidate.MachineId == machineId).ToList();

        message.Should().ContainSingle();
        message[index: 0].GatewayId.Should().Be(machine.GatewayId);
        message[index: 0].CompanyId.Should().Be(tenant.CompanyId);
        message[index: 0].DepartmentId.Should().Be(tenant.FirstDepartmentId);
    }

    [Fact]
    public async Task CreateMachine_WithTakenSerialNumber_StoresNoOutboxRow()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);
        var before = await _fixture.ReadOutboxMessagesAsync<MachineCreated>();

        var act = () => _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.OtherMachineName,
            MachineTestData.SerialNumber);

        await act.Should().ThrowAsync<ConflictException>();
        var after = await _fixture.ReadOutboxMessagesAsync<MachineCreated>();

        after.Should().HaveCount(before.Count);
    }

    [Fact]
    public async Task CreateMachine_Always_StoresOneOperationalStateRow()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var before = TimeProvider.System.GetUtcNow().UtcDateTime;

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);

        var after = TimeProvider.System.GetUtcNow().UtcDateTime;
        var history = await _fixture.LoadStateHistoryAsync(machineId);

        history.Should().ContainSingle();
        history[index: 0].State.Should().Be(MachineState.Operational);
        history[index: 0].StartedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        history[index: 0].DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateMachine_WithSerialNumberInOtherCase_ThrowsConflict()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);

        var act = () => _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.SecondDepartmentId,
            MachineTestData.OtherMachineName,
            MachineTestData.SerialNumberOtherCase);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(MachineErrors.SerialNumberTaken);
    }

    [Fact]
    public async Task CreateMachine_WithSerialNumberOfDeletedMachine_Succeeds()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var firstId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, tenant.BothDepartmentIds, firstId);

        var secondId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumberOtherCase);

        var rows = await _fixture.LoadMachineRowsAsync(tenant.CompanyId, MachineTestData.SerialNumberOtherCase);

        rows.Select(row => row.Id).Should().BeEquivalentTo([firstId, secondId]);
        rows.Count(row => row.DeletedAt is null).Should().Be(expected: 1);
    }

    [Fact]
    public async Task CreateMachine_WithSerialNumberOfOtherCompany_Succeeds()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync(MachineTestData.OtherCompanyName);
        await _fixture.CreateMachineAsync(
            otherTenant.CompanyId,
            otherTenant.BothDepartmentIds,
            otherTenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);

        var machine = await _fixture.LoadMachineRowAsync(machineId);

        machine.CompanyId.Should().Be(tenant.CompanyId);
    }

    [Fact]
    public async Task CreateMachine_WithDescriptionAboveLimit_ThrowsValidation()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var act = () => _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            MachineTestData.TooLongDescription);

        await act.Should().ThrowAsync<AppValidationException>().WithMessage($"*{MachineErrors.DescriptionTooLong}*");
    }

    [Fact]
    public async Task CreateMachine_WithVisibleTextAtLimitInsideFormatting_StoresDescription()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            MachineTestData.FormattedDescriptionAtVisibleLimit);

        var machine = await _fixture.LoadMachineRowAsync(machineId);

        machine.Description.Should().Be(MachineTestData.FormattedDescriptionAtVisibleLimit);
        machine.Description!.Length.Should().BeGreaterThan(MachineTestData.DescriptionLimit);
    }

    [Fact]
    public async Task CreateMachine_WithHostileDescription_ThrowsValidation()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var act = () => _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber,
            MachineTestData.HostileDescription);

        await act.Should()
            .ThrowAsync<AppValidationException>()
            .WithMessage($"*{MachineErrors.DescriptionNotAllowed}*");
    }

    [Fact]
    public async Task CreateMachine_ConcurrentSameSerialNumber_OneSucceedsOneConflicts()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        using var race = _fixture.SaveGate.StartRace(MachineTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => CreateRaceMachineAsync(tenant)),
            race.RunAsync(() => CreateRaceMachineAsync(tenant)));

        var rows = await _fixture.LoadMachineRowsAsync(tenant.CompanyId, MachineTestData.SerialNumberOtherCase);

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(MachineErrors.SerialNumberTaken);
        rows.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateMachine_WithDepartmentOutsideActor_ThrowsForbidden()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var act = () => _fixture.CreateMachineAsync(
            tenant.CompanyId,
            [tenant.FirstDepartmentId],
            tenant.SecondDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        var rows = await _fixture.LoadMachineRowsAsync(tenant.CompanyId, MachineTestData.SerialNumberOtherCase);

        rows.Should().BeEmpty();
    }

    private Task<Guid> CreateRaceMachineAsync(MachineTenant tenant) =>
        _fixture.CreateMachineAsync(
            tenant.CompanyId,
            tenant.BothDepartmentIds,
            tenant.FirstDepartmentId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);
}
