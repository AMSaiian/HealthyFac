using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.Relay.Data;
using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.IntegrationTests.Relay;

[Collection(SeederCollection.Name)]
[Feature(TestFeatures.Seeding)]
[Feature(TestFeatures.Messaging)]
public sealed class DatabaseRelayTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact(Explicit = true)]
    public async Task RunAsync_WithAMachineStateChange_CopiesTheRowToTheReceiverAndPublishesIt()
    {
        await _fixture.InitializeHostsAsync(TestContext.Current.CancellationToken);
        var (companyId, machineId) = await _fixture.CreateMachineAsync();

        await _fixture.Hosts.CreateRelay()
            .RunAsync(eventItem: "machines.json#press-1", TestContext.Current.CancellationToken);

        var queued = await _fixture.QueueStateChangesAsync(companyId, machineId, count: 1);

        await _fixture.Hosts.CreateRelay()
            .RunAsync(eventItem: "machines.json#press-1-state", TestContext.Current.CancellationToken);

        var coreOutbox = await _fixture.LoadOutboxAsync(_fixture.Hosts.Core);
        var telemetryInbox = await _fixture.LoadInboxAsync(_fixture.Hosts.Telemetry);
        var sent = coreOutbox.Single(message => message.Type == MachineStateChanged.MessageType);
        var received = telemetryInbox.Single(message => message.Id == sent.Id);
        received.Type.Should().Be(sent.Type);
        received.Payload.Should().Be(sent.Payload);
        received.ActorId.Should().Be(sent.ActorId);
        received.CompanyId.Should().Be(companyId);
        received.ProcessedOn.Should().NotBeNull();
        sent.ProcessedOn.Should().NotBeNull();
        received.ReadStateChange().StartedAt.Should().Be(queued[0].StartedAt);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_AfterTheCascade_LeavesNoOpenMessageInAnyDatabase()
    {
        await _fixture.InitializeHostsAsync(TestContext.Current.CancellationToken);
        await _fixture.CreateMachineAsync();

        await _fixture.Hosts.CreateRelay()
            .RunAsync(eventItem: "machines.json#press-1", TestContext.Current.CancellationToken);

        var openCount = await _fixture.CountOpenMessagesAsync();
        openCount.Should().Be(expected: 0);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithThreeRowsOfOneSave_DeliversThemInTheirAddOrder()
    {
        await _fixture.InitializeHostsAsync(TestContext.Current.CancellationToken);
        var (companyId, machineId) = await _fixture.CreateMachineAsync();

        await _fixture.Hosts.CreateRelay()
            .RunAsync(eventItem: "machines.json#press-1", TestContext.Current.CancellationToken);

        var queued = await _fixture.QueueStateChangesAsync(companyId, machineId, count: 3);

        await _fixture.Hosts.CreateRelay()
            .RunAsync(eventItem: "machines.json#press-1-state", TestContext.Current.CancellationToken);

        var telemetryInbox = await _fixture.LoadInboxAsync(_fixture.Hosts.Telemetry);

        var receivedStarts = telemetryInbox
            .Where(message => message.Type == MachineStateChanged.MessageType)
            .Select(message => message.ReadStateChange().StartedAt);

        receivedStarts.Should().Equal(queued.Select(message => message.StartedAt));
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithAMessageThatTheReceiverRejects_StopsWithTheTypeTheServiceAndTheError()
    {
        await _fixture.InitializeHostsAsync(TestContext.Current.CancellationToken);
        var (companyId, _) = await _fixture.CreateMachineAsync();

        await _fixture.Hosts.CreateRelay()
            .RunAsync(eventItem: "machines.json#press-1", TestContext.Current.CancellationToken);

        _fixture.EnterNextMoment(item: "unknown-machine");

        await _fixture.AddToCoreOutboxAsync(
            companyId,
            [
                new MachineStateChanged
                {
                    MachineId = Guid.NewGuid(),
                    CompanyId = companyId,
                    State = MachineStateValue.Operational,
                    StartedAt = new DateTime(
                        year: 2027, month: 3, day: 24, hour: 8, minute: 0, second: 0, DateTimeKind.Utc),
                },
            ]);

        var act = () => _fixture.Hosts.CreateRelay()
            .RunAsync(eventItem: "machines.json#bad-state", TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                $"*{RelayTestData.TelemetryService}*{MachineStateChanged.MessageType}*machines.json#bad-state*"
                + $"{MachineRegistrationErrors.RegistrationNotFound}*");
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithAMessageThatFailsItsValidator_StopsWithTheValidationErrorBeforeTheUseCaseRuns()
    {
        await _fixture.InitializeHostsAsync(TestContext.Current.CancellationToken);
        var (companyId, machineId) = await _fixture.CreateMachineAsync();

        await _fixture.Hosts.CreateRelay()
            .RunAsync(eventItem: "machines.json#press-1", TestContext.Current.CancellationToken);

        _fixture.EnterNextMoment(item: "empty-company");

        await _fixture.AddToCoreOutboxAsync(
            companyId,
            [
                new MachineStateChanged
                {
                    MachineId = machineId,
                    CompanyId = Guid.Empty,
                    State = MachineStateValue.Operational,
                    StartedAt = new DateTime(
                        year: 2027, month: 3, day: 24, hour: 8, minute: 0, second: 0, DateTimeKind.Utc),
                },
            ]);

        var act = () => _fixture.Hosts.CreateRelay()
            .RunAsync(eventItem: "machines.json#empty-company", TestContext.Current.CancellationToken);

        var exception = await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                $"*{RelayTestData.TelemetryService}*{MachineStateChanged.MessageType}*machines.json#empty-company*"
                + $"{MachineMessageErrors.Validation.CompanyIdRequired}*");

        exception.Which.Message.Should().NotContain(MachineRegistrationErrors.RegistrationNotFound);
    }

    [Fact(Explicit = true)]
    public async Task TryAddAsync_WithAnIdThatTheInboxHolds_StoresNothing()
    {
        await _fixture.InitializeHostsAsync(TestContext.Current.CancellationToken);
        var messageId = Guid.CreateVersion7();
        using var scope = _fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);
        var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();

        await inbox.TryAddAsync(
            messageId, type: "core.v1:Other", payload: "{}", traceParent: null, Guid.NewGuid(), companyId: null,
            TestContext.Current.CancellationToken);

        var isStored = await inbox.TryAddAsync(
            messageId, type: "core.v1:Other", payload: "{}", traceParent: null, Guid.NewGuid(), companyId: null,
            TestContext.Current.CancellationToken);

        isStored.Should().BeFalse();
    }
}
