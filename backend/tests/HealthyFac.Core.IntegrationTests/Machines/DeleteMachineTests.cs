using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Drafts.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Machines;

[Collection(CoreMachinesCollection.Name)]
public sealed class DeleteMachineTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task DeleteMachine_Always_HidesMachine()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);

        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        var machine = await _fixture.LoadMachineRowAsync(machineId);
        var get = () => _fixture.GetMachineAsync(tenant.CompanyId, machineId);

        machine.DeletedAt.Should().NotBeNull();
        machine.DeletedBy.Should().NotBeNull();
        await get.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteMachine_WithActiveMachine_StoresMachineDeletedOutboxRow()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);

        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        var messages = await _fixture.ReadOutboxMessagesAsync<MachineDeleted>();
        var message = messages.Where(candidate => candidate.MachineId == machineId).ToList();

        message.Should().ContainSingle();
        message[0].CompanyId.Should().Be(tenant.CompanyId);
    }

    [Fact]
    public async Task DeleteMachine_InOtherCompany_StoresNoOutboxRow()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync(MachineTestData.OtherCompanyName);
        var machineId = await ArrangeMachineAsync(otherTenant);

        var act = () => _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        await act.Should().ThrowAsync<NotFoundException>();
        var messages = await _fixture.ReadOutboxMessagesAsync<MachineDeleted>();

        messages.Should().NotContain(candidate => candidate.MachineId == machineId);
    }

    [Fact]
    public async Task DeleteMachine_Twice_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);
        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        var act = () => _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
    }

    [Fact]
    public async Task DeleteMachine_InOtherCompany_ThrowsNotFound()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var otherTenant = await _fixture.ArrangeMachineTenantAsync(MachineTestData.OtherCompanyName);
        var machineId = await ArrangeMachineAsync(otherTenant);

        var act = () => _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineErrors.MachineNotFound);
        var machine = await _fixture.LoadMachineRowAsync(machineId);

        machine.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task DeleteMachine_Always_SoftDeletesStateHistory()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);

        var otherMachineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            MachineTestData.OtherMachineName,
            MachineTestData.OtherSerialNumber);

        var firstHistory = await _fixture.LoadStateHistoryAsync(machineId);

        await _fixture.AddStateRowAsync(
            machineId,
            MachineState.InMaintenance,
            firstHistory[0].StartedAt.AddDays(MachineTestData.EarlierStateDays));

        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        var history = await _fixture.LoadStateHistoryAsync(machineId);
        var otherHistory = await _fixture.LoadStateHistoryAsync(otherMachineId);

        history.Should().HaveCount(expected: 2).And.OnlyContain(row => row.DeletedAt != null);
        otherHistory.Should().ContainSingle().Which.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Delete_MachineWithStateHistory_SoftDeletesHistoryWithoutLoadingIt()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);
        var firstHistory = await _fixture.LoadStateHistoryAsync(machineId);

        await _fixture.AddStateRowAsync(
            machineId,
            MachineState.InMaintenance,
            firstHistory[0].StartedAt.AddDays(MachineTestData.EarlierStateDays));

        _fixture.Clock.Freeze(SoftDeleteQueryTestData.FirstDeleteAt);
        using var scope = _fixture.CreateUserScope(tenant.CompanyId);

        await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<DeleteMachineRequest>>()
            .HandleAsync(
                new DeleteMachineRequest { CompanyId = tenant.CompanyId, Id = machineId },
                TestContext.Current.CancellationToken);

        var actorId = scope.ServiceProvider.GetRequiredService<ICurrentUserProvider>().TryGetUserId();

        var trackedHistory = scope.ServiceProvider.GetRequiredService<CoreDbContext>()
            .ChangeTracker
            .Entries<MachineStateHistory>()
            .ToList();

        var history = await _fixture.LoadStateHistoryAsync(machineId);

        trackedHistory.Should().BeEmpty();

        history.Should()
            .HaveCount(expected: 2)
            .And.OnlyContain(row => row.DeletedAt == SoftDeleteQueryTestData.FirstDeleteAt.UtcDateTime
                && row.DeletedBy == actorId
                && row.UpdatedAt == SoftDeleteQueryTestData.FirstDeleteAt.UtcDateTime
                && row.UpdatedBy == actorId);
    }

    [Fact]
    public async Task Delete_WhenOuterTransactionRollsBack_LeavesHistoryUnchanged()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);
        using var scope = _fixture.CreateUserScope(tenant.CompanyId);
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<DeleteMachineRequest>>();

        var act = () => dbContext.RunInTransactionAsync(
            async token =>
            {
                await handler.HandleAsync(
                    new DeleteMachineRequest { CompanyId = tenant.CompanyId, Id = machineId }, token);

                throw new TransactionTestException();
            }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<TransactionTestException>();

        var machine = await _fixture.LoadMachineRowAsync(machineId);
        var history = await _fixture.LoadStateHistoryAsync(machineId);

        machine.DeletedAt.Should().BeNull();
        history.Should().OnlyContain(row => row.DeletedAt == null);
    }

    [Fact]
    public async Task DeleteMachine_WithAPendingMachineDraft_DismissesIt()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await ArrangeMachineAsync(tenant);
        await _fixture.AddDraftsForMachineAsync(tenant.CompanyId, machineId);

        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        var machineDrafts = await _fixture.LoadMachineDraftsAsync();

        machineDrafts.Should().ContainSingle().Which.State.Should().Be(DraftState.Dismissed);
    }

    [Fact]
    public async Task DeleteMachine_WhenUpdateAndDeleteOverlap_OneSucceedsAndOneConflicts()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, MachineTestData.MachineName, MachineTestData.SerialNumber);

        using var race = _fixture.SaveGate.StartRace(MachineTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.DeleteMachineAsync(tenant.CompanyId, machineId)),
            race.RunAsync(() => _fixture.UpdateMachineAsync(
                tenant.CompanyId, machineId, MachineTestData.MachineName, MachineTestData.SerialNumber,
                description: "Second")));

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);

        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(MachineErrors.MachineChanged);
    }

    private Task<Guid> ArrangeMachineAsync(MachineTenant tenant) =>
        _fixture.CreateMachineAsync(
            tenant.CompanyId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);
}
