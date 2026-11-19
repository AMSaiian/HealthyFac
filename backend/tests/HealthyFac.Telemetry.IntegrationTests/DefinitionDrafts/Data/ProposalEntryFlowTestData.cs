using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

/// <summary>The fixed IDs of one proposal entry and its two children.</summary>
public sealed record FlowIds
{
    public required Guid EntryId { get; init; }

    public required Guid MetricId { get; init; }

    public required Guid StatusId { get; init; }

    public required Guid PhaseId { get; init; }

    public required Guid MachineId { get; init; }

    public static FlowIds Create() =>
        new()
        {
            EntryId = Guid.CreateVersion7(),
            MetricId = Guid.CreateVersion7(),
            StatusId = Guid.CreateVersion7(),
            PhaseId = Guid.CreateVersion7(),
            MachineId = Guid.CreateVersion7(),
        };
}

/// <summary>One message that a flow test delivers.</summary>
public delegate Task FlowDelivery(TelemetryDatabaseFixture fixture, FlowIds ids);

public static class ProposalEntryFlowTestData
{
    public static readonly FlowDelivery Proposed = (fixture, ids) =>
        fixture.StoreProposedDefinitionDraftsAsync(
            DefinitionDraftTestData.CompanyId,
            ProposalBuilder.Message(DefinitionDraftTestData.CompanyId, ProposedMachineOf(ids)));

    public static readonly FlowDelivery AcceptedOnMachineDraft = (fixture, ids) =>
        fixture.ResolveDraftsAsync(Resolved(DraftResolution.Accepted, ids, isOnMachineDraft: true, machineId: null));

    public static readonly FlowDelivery AcceptedOnMachine = (fixture, ids) =>
        fixture.ResolveDraftsAsync(
            Resolved(DraftResolution.Accepted, ids, isOnMachineDraft: false, ids.MachineId));

    public static readonly FlowDelivery Rejected = (fixture, ids) =>
        fixture.ResolveDraftsAsync(Resolved(DraftResolution.Rejected, ids, isOnMachineDraft: false, machineId: null));

    public static readonly FlowDelivery Applied = (fixture, ids) =>
        fixture.ApplyMachineDraftMessageAsync(
            new MachineDraftApplied
            {
                DraftId = ids.EntryId,
                FileId = DefinitionDraftTestData.FileId,
                MachineId = ids.MachineId,
                CompanyId = DefinitionDraftTestData.CompanyId,
                QueuedAt =
                    new DateTime(year: 2026, month: 11, day: 17, hour: 8, minute: 0, second: 0, DateTimeKind.Utc),
            });

    public static readonly FlowDelivery Dismissed = (fixture, ids) =>
        fixture.DismissMachineDraftMessageAsync(
            new MachineDraftDismissed { DraftId = ids.EntryId, CompanyId = DefinitionDraftTestData.CompanyId });

    /// <summary>The case name and the 3 messages in one order. All 6 orders of the apply flow.</summary>
    public static TheoryData<string, FlowDelivery[]> ApplyOrders => Orders(Proposed, AcceptedOnMachineDraft, Applied);

    /// <summary>The case name and the 3 messages in one order. All 6 orders of the dismiss flow.</summary>
    public static TheoryData<string, FlowDelivery[]> DismissOrders =>
        Orders(Proposed, AcceptedOnMachineDraft, Dismissed);

    /// <summary>The case name, the 2 messages in one order, and how many rows each kind keeps.</summary>
    public static TheoryData<string, FlowDelivery[], int> RejectOrders => new()
    {
        { "proposed, rejected", [Proposed, Rejected], 1 },
        { "rejected, proposed", [Rejected, Proposed], 0 },
    };

    /// <summary>The case name and one message that is delivered twice.</summary>
    public static TheoryData<string, FlowDelivery> RepeatedMessages => new()
    {
        { "proposed", Proposed },
        { "accepted on machine draft", AcceptedOnMachineDraft },
        { "applied", Applied },
        { "dismissed", Dismissed },
    };

    private static TheoryData<string, FlowDelivery[]> Orders(
        FlowDelivery first,
        FlowDelivery second,
        FlowDelivery third
    ) =>
        new()
        {
            { "first, second, third", [first, second, third] },
            { "first, third, second", [first, third, second] },
            { "second, first, third", [second, first, third] },
            { "second, third, first", [second, third, first] },
            { "third, first, second", [third, first, second] },
            { "third, second, first", [third, second, first] },
        };

    private static ProposedMachine ProposedMachineOf(FlowIds ids) =>
        new()
        {
            DraftId = ids.EntryId,
            Metrics = [new ProposedMetric { DraftId = ids.MetricId, Key = "motor.temperature" }],
            Statuses = [new ProposedStatus { DraftId = ids.StatusId, Code = "E104" }],
            Phases = [PhaseDraftTestData.ProposedPhase(ids)],
        };

    private static DraftsResolved Resolved(
        DraftResolution resolution,
        FlowIds ids,
        bool isOnMachineDraft,
        Guid? machineId
    ) =>
        new()
        {
            AttemptId = DefinitionDraftTestData.AttemptId,
            CompanyId = DefinitionDraftTestData.CompanyId,
            Resolution = resolution,
            Entries =
            [
                new ResolvedEntry { EntryId = ids.EntryId, MachineId = machineId, IsOnMachineDraft = isOnMachineDraft },
            ],
        };
}
