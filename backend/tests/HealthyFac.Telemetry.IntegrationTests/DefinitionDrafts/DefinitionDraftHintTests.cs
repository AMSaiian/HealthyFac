using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
public sealed class DefinitionDraftHintTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ResolveDrafts_WithAMachineEntry_RecordsTheHintWithTheAttemptAndTheMachine()
    {
        var machineId = Guid.CreateVersion7();
        var message = new DraftsResolved
        {
            AttemptId = DefinitionDraftTestData.AttemptId,
            CompanyId = DefinitionDraftTestData.CompanyId,
            Resolution = DraftResolution.Accepted,
            Entries = [new ResolvedEntry { EntryId = Guid.CreateVersion7(), MachineId = machineId }],
        };

        await HandleAndPushAsync(message);

        AssertOneHint(DefinitionDraftTestData.CompanyId, DefinitionDraftTestData.AttemptId, machineId);
    }

    [Fact]
    public async Task ApplyMachineDraft_WithAMachine_RecordsTheHintWithTheMachine()
    {
        var machineId = Guid.CreateVersion7();
        var message = new MachineDraftApplied
        {
            DraftId = Guid.CreateVersion7(),
            FileId = DefinitionDraftTestData.FileId,
            MachineId = machineId,
            CompanyId = DefinitionDraftTestData.CompanyId,
            QueuedAt = new DateTime(year: 2026, month: 11, day: 17, hour: 8, minute: 0, second: 0, DateTimeKind.Utc),
        };

        await HandleAndPushAsync(message);

        AssertOneHint(DefinitionDraftTestData.CompanyId, attemptId: null, machineId);
    }

    [Fact]
    public async Task RegisterMachine_WithANewMachine_RecordsTheHintWithTheMachine()
    {
        var message = MachineRegistrationTestData.CreateMessage();

        await HandleAndPushAsync(message);

        AssertOneHint(message.CompanyId, attemptId: null, message.MachineId);
    }

    [Fact]
    public async Task RegisterMachine_WithARepeat_RecordsNoSecondHint()
    {
        var message = MachineRegistrationTestData.CreateMessage();
        await HandleAndPushAsync(message);
        _fixture.RealtimeQueue.Clear();

        await HandleAndPushAsync(message);

        _fixture.RealtimeQueue.Items.Should().BeEmpty();
    }

    private async Task HandleAndPushAsync<TMessage>(TMessage message)
        where TMessage : IRequest
    {
        using var scope = _fixture.ScopeFactory.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IRequestHandler<TMessage>>().HandleAsync(message);
        scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>().Push();
    }

    private void AssertOneHint(Guid companyId, Guid? attemptId, Guid machineId)
    {
        var push = _fixture.RealtimeQueue.Items.Should().ContainSingle().Subject.Push;
        push.Group.Should().Be(RealtimeGroups.ForTopic(companyId, RealtimeTopic.DefinitionDrafts));
        push.Hint!.AttemptId.Should().Be(attemptId);
        push.Hint.MachineId.Should().Be(machineId);
    }
}
