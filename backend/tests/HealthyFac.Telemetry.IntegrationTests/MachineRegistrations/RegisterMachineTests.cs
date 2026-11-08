using System.Text.Json.Nodes;
using HealthyFac.Shared.Domain.Gateways;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.Intake;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;

namespace HealthyFac.Telemetry.IntegrationTests.MachineRegistrations;

[Collection(TelemetryIntakeCollection.Name)]
public sealed class RegisterMachineTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task RegisterMachine_WithNewMachine_CreatesRegistration()
    {
        var message = MachineRegistrationTestData.CreateMessage();

        await _fixture.RegisterMachineAsync(message);

        var registration = await _fixture.LoadRegistrationAsync(message.MachineId);

        registration.Should().NotBeNull();
        registration!.GatewayId.Should().Be(message.GatewayId);
        registration.CompanyId.Should().Be(message.CompanyId);
        registration.MetricSchemaRevision.Should().Be(expected: 0);
        registration.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task RegisterMachine_WithNewMachine_StoresRevision0Schema()
    {
        var message = MachineRegistrationTestData.CreateMessage();

        await _fixture.RegisterMachineAsync(message);

        var revisions = await _fixture.LoadSchemaRevisionsAsync(message.MachineId);
        var schema = JsonNode.Parse(revisions.Single().Schema)!;

        revisions.Single().Revision.Should().Be(expected: 0);
        schema[propertyName: "$id"]!.GetValue<string>()
            .Should()
            .Be($"urn:healthyfac:gateways:{message.GatewayId}:metrics:0");
        schema[propertyName: "properties"]![propertyName: "metrics"]![propertyName: "properties"]!.AsObject()
            .Should()
            .BeEmpty();
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

        registration!.GatewayId.Should().Be(message.GatewayId);
        registration.CompanyId.Should().Be(message.CompanyId);
    }

    [Fact]
    public async Task RegisterMachine_Twice_CreatesOneRow()
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
