using HealthyFac.Document.IntegrationTests.Database;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Document.IntegrationTests.MachineRegistrations.Data;

namespace HealthyFac.Document.IntegrationTests.MachineRegistrations;

[Collection(DocumentDatabaseCollection.Name)]
public sealed class RenameMachineTests(DocumentDatabaseFixture fixture)
{
    private const string NewName = "Renamed Pump";
    private const string OlderName = "Old Pump";

    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task RenameMachine_WithNewerRename_UpdatesName()
    {
        var created = MachineRegistrationTestData.CreateMessage();
        await _fixture.RegisterMachineAsync(created);

        await _fixture.RenameMachineAsync(
            MachineRegistrationTestData.CreateRenamedMessage(
                created.MachineId, NewName, MachineRegistrationTestData.RenamedAt));

        var registration = await _fixture.LoadRegistrationAsync(created.MachineId);

        registration!.Name.Should().Be(NewName);
        registration.NameChangedAt.Should().Be(MachineRegistrationTestData.RenamedAt);
    }

    [Fact]
    public async Task RenameMachine_WithOlderRename_ChangesNothing()
    {
        var created = MachineRegistrationTestData.CreateMessage();
        await _fixture.RegisterMachineAsync(created);
        await _fixture.RenameMachineAsync(
            MachineRegistrationTestData.CreateRenamedMessage(
                created.MachineId, NewName, MachineRegistrationTestData.RenamedAt));

        await _fixture.RenameMachineAsync(
            MachineRegistrationTestData.CreateRenamedMessage(
                created.MachineId, OlderName, MachineRegistrationTestData.RenamedAt.AddMinutes(value: -1)));

        var registration = await _fixture.LoadRegistrationAsync(created.MachineId);

        registration!.Name.Should().Be(NewName);
    }

    [Fact]
    public async Task RenameMachine_WithoutRegistration_CompletesWithoutRow()
    {
        var machineId = Guid.CreateVersion7();

        await _fixture.RenameMachineAsync(
            MachineRegistrationTestData.CreateRenamedMessage(
                machineId, NewName, MachineRegistrationTestData.RenamedAt));

        var count = await _fixture.CountRegistrationsAsync(machineId);

        count.Should().Be(expected: 0);
    }
}
