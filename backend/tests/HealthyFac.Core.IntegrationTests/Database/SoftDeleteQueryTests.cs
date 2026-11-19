using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Persistence.SoftDelete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Database;

[Collection(CoreMachinesCollection.Name)]
public sealed class SoftDeleteQueryTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SoftDeleteAsync_InsideTransaction_SetsDeleteAndAuditFields()
    {
        var machineId = await ArrangeMachineWithTwoStatesAsync();
        _fixture.Clock.Freeze(SoftDeleteQueryTestData.FirstDeleteAt);

        var actorId = await SoftDeleteHistoryInTransactionAsync(machineId, isRollingBack: false);

        var history = await _fixture.LoadStateHistoryAsync(machineId);

        history.Should()
            .HaveCount(expected: 2)
            .And.OnlyContain(row => row.DeletedAt == SoftDeleteQueryTestData.FirstDeleteAt.UtcDateTime
                && row.UpdatedAt == SoftDeleteQueryTestData.FirstDeleteAt.UtcDateTime
                && row.DeletedBy == actorId
                && row.UpdatedBy == actorId);
    }

    [Fact]
    public async Task SoftDeleteAsync_WithDeletedRow_KeepsItsDeleteTime()
    {
        var machineId = await ArrangeMachineWithTwoStatesAsync();
        var firstHistory = await _fixture.LoadStateHistoryAsync(machineId);
        _fixture.Clock.Freeze(SoftDeleteQueryTestData.FirstDeleteAt);
        await _fixture.SoftDeleteStateRowAsync(machineId, firstHistory[index: 0].StartedAt);
        _fixture.Clock.Freeze(SoftDeleteQueryTestData.SecondDeleteAt);

        await SoftDeleteHistoryInTransactionAsync(machineId, isRollingBack: false);

        var history = await _fixture.LoadStateHistoryAsync(machineId);

        history[index: 0].DeletedAt.Should().Be(SoftDeleteQueryTestData.FirstDeleteAt.UtcDateTime);
        history[index: 1].DeletedAt.Should().Be(SoftDeleteQueryTestData.SecondDeleteAt.UtcDateTime);
    }

    [Fact]
    public async Task SoftDeleteAsync_WithoutTransaction_ThrowsAndChangesNoRow()
    {
        var machineId = await ArrangeMachineWithTwoStatesAsync();
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

        var act = () => dbContext.MachineStateHistories
            .Where(row => row.MachineId == machineId)
            .SoftDeleteAsync(
                dbContext,
                scope.ServiceProvider.GetRequiredService<TimeProvider>(),
                scope.ServiceProvider.GetRequiredService<ICurrentUserProvider>(),
                TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(SoftDeleteErrors.NoOpenTransaction().Message);

        var history = await _fixture.LoadStateHistoryAsync(machineId);

        history.Should().OnlyContain(row => row.DeletedAt == null);
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenTransactionRollsBack_ChangesNoRow()
    {
        var machineId = await ArrangeMachineWithTwoStatesAsync();

        var act = () => SoftDeleteHistoryInTransactionAsync(machineId, isRollingBack: true);

        await act.Should().ThrowAsync<TransactionTestException>();

        var history = await _fixture.LoadStateHistoryAsync(machineId);

        history.Should().OnlyContain(row => row.DeletedAt == null && row.UpdatedAt == null);
    }

    private async Task<Guid> ArrangeMachineWithTwoStatesAsync()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();
        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId,
            MachineTestData.MachineName,
            MachineTestData.SerialNumber);
        var firstHistory = await _fixture.LoadStateHistoryAsync(machineId);
        await _fixture.AddStateRowAsync(
            machineId,
            MachineState.InMaintenance,
            firstHistory[index: 0].StartedAt.AddDays(SoftDeleteQueryTestData.EarlierStateDays));

        return machineId;
    }

    private async Task<Guid?> SoftDeleteHistoryInTransactionAsync(Guid machineId, bool isRollingBack)
    {
        var companyId = await LoadCompanyIdAsync(machineId);
        using var scope = _fixture.CreateUserScope(companyId);
        var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var currentUserProvider = scope.ServiceProvider.GetRequiredService<ICurrentUserProvider>();

        await dbContext.RunInTransactionAsync(
            async token =>
            {
                await dbContext.MachineStateHistories
                    .Where(row => row.MachineId == machineId)
                    .SoftDeleteAsync(dbContext, timeProvider, currentUserProvider, token);

                if (isRollingBack)
                {
                    throw new TransactionTestException();
                }
            }, TestContext.Current.CancellationToken);

        return currentUserProvider.TryGetUserId();
    }

    private async Task<Guid> LoadCompanyIdAsync(Guid machineId)
    {
        using var scope = _fixture.ScopeFactory.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
            .Machines
            .Where(machine => machine.Id == machineId)
            .Select(machine => machine.CompanyId)
            .SingleAsync();
    }
}
