using HealthyFac.Document.IntegrationTests.Database;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Document.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Shared.Domain.Gateways;

namespace HealthyFac.Document.IntegrationTests.MachineRegistrations;

[Collection(DocumentDatabaseCollection.Name)]
public sealed class RegisterMachineTests(DocumentDatabaseFixture fixture)
{
    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task RegisterMachine_WithNewMachine_CreatesRegistration()
    {
        var message = MachineRegistrationTestData.CreateMessage();

        await _fixture.RegisterMachineAsync(message);

        var registration = await _fixture.LoadRegistrationAsync(message.MachineId);

        registration.Should().NotBeNull();
        registration!.CompanyId.Should().Be(message.CompanyId);
        registration.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task RegisterMachine_WithExistingRegistration_ChangesNothing()
    {
        var message = MachineRegistrationTestData.CreateMessage();
        await _fixture.RegisterMachineAsync(message);
        var changed = message with
        {
            GatewayId = GatewayIds.Create(),
            CompanyId = MachineRegistrationTestData.OtherCompanyId,
        };

        await _fixture.RegisterMachineAsync(changed);

        var registration = await _fixture.LoadRegistrationAsync(message.MachineId);

        registration!.CompanyId.Should().Be(message.CompanyId);
    }

    [Fact]
    public async Task RegisterMachine_WithDuplicateMessageId_CreatesOneRow()
    {
        var message = MachineRegistrationTestData.CreateMessage();

        await _fixture.RegisterMachineAsync(message);
        await _fixture.RegisterMachineAsync(message);

        var count = await _fixture.CountRegistrationsAsync(message.MachineId);

        count.Should().Be(expected: 1);
    }

    [Fact]
    public async Task RegisterMachine_InParallel_CreatesOneRowAndBothSucceed()
    {
        var message = MachineRegistrationTestData.CreateMessage();
        using var race = _fixture.SaveGate.StartRace(MachineRegistrationTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.RegisterMachineAsync(message)),
            race.RunAsync(() => _fixture.RegisterMachineAsync(message)));

        var count = await _fixture.CountRegistrationsAsync(message.MachineId);

        race.SaveCounts.Should().Equal(1, 1);
        failures.Should().OnlyContain(failure => failure == null);
        count.Should().Be(expected: 1);
    }
}
