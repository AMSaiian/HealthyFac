using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.IntegrationTests.Machines;

[Collection(CoreMachinesCollection.Name)]
[Feature(TestFeatures.Machines)]
public sealed class GetMachineStateTimesTests(CoreDatabaseFixture fixture)
{
    private const int RangeHours = 2;

    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetMachineStateTimes_WithStatesThatCrossTheRange_ClipsEachStateAtTheRangeBounds()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, MachineTestData.MachineName, MachineTestData.SerialNumber,
            MachineTestData.LinkDescription);

        var created = (await _fixture.LoadStateHistoryAsync(machineId)).Min(row => row.StartedAt);
        var from = created.AddHours(value: 1);
        await _fixture.AddStateRowAsync(machineId, MachineState.InMaintenance, from.AddMinutes(value: 30));
        await _fixture.AddStateRowAsync(machineId, MachineState.Operational, from.AddMinutes(value: 90));

        var times = await _fixture.GetMachineStateTimesAsync(
            tenant.CompanyId, machineId, from, from.AddHours(RangeHours));

        times.States.Single(state => state.State == MachineState.InMaintenance.Value).Seconds.Should().Be(60 * 60);
        times.States.Single(state => state.State == MachineState.Operational.Value).Seconds.Should().Be(60 * 60);
        times.BeforeCreationSeconds.Should().Be(expected: 0);
    }

    [Fact]
    public async Task GetMachineStateTimes_WithARangeThatStartsBeforeTheCreation_CountsTheTimeBeforeTheCreation()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, MachineTestData.MachineName, MachineTestData.SerialNumber,
            MachineTestData.LinkDescription);

        var created = (await _fixture.LoadStateHistoryAsync(machineId)).Min(row => row.StartedAt);

        var times = await _fixture.GetMachineStateTimesAsync(
            tenant.CompanyId, machineId, created.AddHours(value: -1), created.AddHours(value: 1));

        times.BeforeCreationSeconds.Should().Be(60 * 60);
        times.States.Sum(state => state.Seconds).Should().Be(60 * 60);
    }

    [Fact]
    public async Task GetMachineStateTimes_WithAnotherCompany_ThrowsNotFoundException()
    {
        var tenant = await _fixture.ArrangeMachineTenantAsync();

        var machineId = await _fixture.CreateMachineAsync(
            tenant.CompanyId, MachineTestData.MachineName, MachineTestData.SerialNumber,
            MachineTestData.LinkDescription);

        var otherTenant = await _fixture.ArrangeMachineTenantAsync(companyName: "Other company");
        var from = MachineTestData.FirstCreatedAt;

        var act = () => _fixture.GetMachineStateTimesAsync(
            otherTenant.CompanyId, machineId, from, from.AddHours(RangeHours));

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
