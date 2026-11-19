using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Core.Messaging.Machines;

namespace HealthyFac.Core.IntegrationTests.Database;

[Collection(CoreSeedingCollection.Name)]
public sealed class CoreSeedingMessageTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SeedOptionalData_WithSeedMachines_QueuesOneCreatedMessageWithTheStoredGoLiveForEachMachine()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);

        var rows = await _fixture.LoadSeededGoLiveRowsAsync();
        var messages = await _fixture.ReadOutboxMessagesAsync<MachineCreated>();
        var expected = rows.Select(row => new
        {
            row.MachineId,
            row.CompanyId,
            row.ExpectedGoLiveOn,
            GoLiveStartsAt = MachineGoLiveRules.StartOfDayUtc(
                row.ExpectedGoLiveOn,
                TimeZoneInfo.FindSystemTimeZoneById(row.TimeZoneId)),
        });
        var actual = messages.Select(message => new
        {
            message.MachineId,
            message.CompanyId,
            ExpectedGoLiveOn = message.ExpectedGoLiveOn,
            GoLiveStartsAt = message.GoLiveStartsAt,
        });

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SeedOptionalData_WithSeedMachines_QueuesOneStateMessageWithTheCurrentStateForEachMachine()
    {
        await _fixture.RunSeederAsync(DatabaseTestData.ForcedRecreationWithSeedMachinesOptions);

        var rows = await _fixture.LoadSeededGoLiveRowsAsync();
        var messages = await _fixture.ReadOutboxMessagesAsync<MachineStateChanged>();
        var expected = new List<MachineStateChanged>();

        foreach (var row in rows)
        {
            var history = await _fixture.LoadStateHistoryAsync(row.MachineId);
            var current = history.MaxBy(item => item.StartedAt)!;
            expected.Add(
                new MachineStateChanged
                {
                    MachineId = row.MachineId,
                    CompanyId = row.CompanyId,
                    State = MachineStateValue.FromValue(current.State.Value),
                    StartedAt = current.StartedAt,
                });
        }

        messages.Should()
            .BeEquivalentTo(
                expected,
                options => options.Using<DateTime>(context => context.Subject.Should()
                        .BeCloseTo(
                            context.Expectation, TimeSpan.FromMilliseconds(milliseconds: 1)))
                    .WhenTypeIs<DateTime>());
    }
}
