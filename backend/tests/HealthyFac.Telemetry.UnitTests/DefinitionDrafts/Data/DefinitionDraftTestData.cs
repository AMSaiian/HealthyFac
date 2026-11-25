using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.StatusDrafts;

namespace HealthyFac.Telemetry.UnitTests.DefinitionDrafts.Data;

/// <summary>A draft setup and the result that one <c>Follow</c> call must give.</summary>
public sealed record FollowCase
{
    public required ProposalEntryState EntryState { get; init; }

    public required DefinitionDraftState ChildState { get; init; }

    public Guid? TentativeMachineId { get; init; }

    public bool IsTargetRegistrationDeleted { get; init; }

    public required DefinitionDraftState ExpectedState { get; init; }

    public Guid? ExpectedMachineId { get; init; }

    public Guid? ExpectedMachineDraftId { get; init; }

    public bool IsRemovalExpected { get; init; }
}

public static class DefinitionDraftTestData
{
    private static readonly Guid EntryId = ProposalEntryTestData.EntryId;
    private static readonly Guid MachineId = ProposalEntryTestData.MachineId;
    private static readonly Guid OtherMachineId = ProposalEntryTestData.OtherMachineId;

    public static TheoryData<string, FollowCase> FollowCases => new()
    {
        {
            "awaiting entry leaves an awaiting child",
            Unchanged(ProposalEntryState.Awaiting, DefinitionDraftState.AwaitingTarget)
        },
        {
            "awaiting entry leaves a pending child",
            Unchanged(ProposalEntryState.Awaiting, DefinitionDraftState.Pending)
        },
        {
            "rejected entry removes an awaiting child",
            Unchanged(ProposalEntryState.Rejected, DefinitionDraftState.AwaitingTarget) with
            {
                IsRemovalExpected = true,
            }
        },
        {
            "rejected entry leaves a pending child",
            Unchanged(ProposalEntryState.Rejected, DefinitionDraftState.Pending)
        },
        {
            "rejected entry leaves an applied child",
            Unchanged(ProposalEntryState.Rejected, DefinitionDraftState.Applied)
        },
        {
            "onMachineDraft entry moves an awaiting child to the machine draft",
            new FollowCase
            {
                EntryState = ProposalEntryState.OnMachineDraft,
                ChildState = DefinitionDraftState.AwaitingTarget,
                TentativeMachineId = OtherMachineId,
                ExpectedState = DefinitionDraftState.Pending,
                ExpectedMachineDraftId = EntryId,
            }
        },
        {
            "onMachineDraft entry keeps a pending child pending on the machine draft",
            new FollowCase
            {
                EntryState = ProposalEntryState.OnMachineDraft,
                ChildState = DefinitionDraftState.Pending,
                ExpectedState = DefinitionDraftState.Pending,
                ExpectedMachineDraftId = EntryId,
            }
        },
        {
            "onMachineDraft entry leaves an applied child",
            Unchanged(ProposalEntryState.OnMachineDraft, DefinitionDraftState.Applied)
        },
        {
            "onMachineDraft entry leaves a dismissed child",
            Unchanged(ProposalEntryState.OnMachineDraft, DefinitionDraftState.Dismissed)
        },
        {
            "onMachine entry moves an awaiting child to the machine",
            new FollowCase
            {
                EntryState = ProposalEntryState.OnMachine,
                ChildState = DefinitionDraftState.AwaitingTarget,
                ExpectedState = DefinitionDraftState.Pending,
                ExpectedMachineId = MachineId,
            }
        },
        {
            "onMachine entry replaces the tentative machine",
            new FollowCase
            {
                EntryState = ProposalEntryState.OnMachine,
                ChildState = DefinitionDraftState.AwaitingTarget,
                TentativeMachineId = OtherMachineId,
                ExpectedState = DefinitionDraftState.Pending,
                ExpectedMachineId = MachineId,
            }
        },
        {
            "onMachine entry with a deleted registration dismisses an awaiting child",
            new FollowCase
            {
                EntryState = ProposalEntryState.OnMachine,
                ChildState = DefinitionDraftState.AwaitingTarget,
                IsTargetRegistrationDeleted = true,
                ExpectedState = DefinitionDraftState.Dismissed,
                ExpectedMachineId = MachineId,
            }
        },
        {
            "onMachine entry leaves an applied child",
            Unchanged(ProposalEntryState.OnMachine, DefinitionDraftState.Applied)
        },
        {
            "onMachine entry leaves a dismissed child",
            Unchanged(ProposalEntryState.OnMachine, DefinitionDraftState.Dismissed)
        },
        {
            "machineDraftDismissed entry dismisses an awaiting child without a machine",
            new FollowCase
            {
                EntryState = ProposalEntryState.MachineDraftDismissed,
                ChildState = DefinitionDraftState.AwaitingTarget,
                ExpectedState = DefinitionDraftState.Dismissed,
            }
        },
        {
            "machineDraftDismissed entry keeps a child that has a machine",
            new FollowCase
            {
                EntryState = ProposalEntryState.MachineDraftDismissed,
                ChildState = DefinitionDraftState.AwaitingTarget,
                TentativeMachineId = OtherMachineId,
                ExpectedState = DefinitionDraftState.AwaitingTarget,
                ExpectedMachineId = OtherMachineId,
            }
        },
        {
            "machineDraftDismissed entry dismisses a pending child of the machine draft",
            new FollowCase
            {
                EntryState = ProposalEntryState.MachineDraftDismissed,
                ChildState = DefinitionDraftState.Pending,
                ExpectedState = DefinitionDraftState.Dismissed,
                ExpectedMachineDraftId = EntryId,
            }
        },
        {
            "machineDraftDismissed entry leaves an applied child",
            Unchanged(ProposalEntryState.MachineDraftDismissed, DefinitionDraftState.Applied)
        },
    };

    public static TheoryData<string, DefinitionDraftState> NotPendingStates => new()
    {
        { "awaitingTarget", DefinitionDraftState.AwaitingTarget },
        { "applied", DefinitionDraftState.Applied },
        { "dismissed", DefinitionDraftState.Dismissed },
    };

    public static TheoryData<string, DefinitionDraftState> NotOpenStates => new()
    {
        { "applied", DefinitionDraftState.Applied },
        { "dismissed", DefinitionDraftState.Dismissed },
    };

    public static MetricDefinitionDraft CreateMetric(DefinitionDraftState state, Guid? tentativeMachineId = null)
    {
        var draft = MetricDefinitionDraft.Propose(
            ProposalEntryTestData.DraftId,
            ProposalEntryTestData.CompanyId,
            ProposalEntryTestData.AttemptId,
            EntryId,
            tentativeMachineId,
            new MetricDraftValues { Key = "temperature" });

        Reach(draft, state);

        return draft;
    }

    public static StatusDefinitionDraft CreateStatus(DefinitionDraftState state, Guid? tentativeMachineId = null)
    {
        var draft = StatusDefinitionDraft.Propose(
            ProposalEntryTestData.DraftId,
            ProposalEntryTestData.CompanyId,
            ProposalEntryTestData.AttemptId,
            EntryId,
            tentativeMachineId,
            new StatusDraftValues { Code = "E1" });

        Reach(draft, state);

        return draft;
    }

    private static FollowCase Unchanged(ProposalEntryState entryState, DefinitionDraftState childState) =>
        new()
        {
            EntryState = entryState,
            ChildState = childState,
            ExpectedState = childState,
            ExpectedMachineDraftId = childState == DefinitionDraftState.AwaitingTarget ? null : EntryId,
        };

    private static void Reach(IDefinitionDraft draft, DefinitionDraftState state)
    {
        if (state == DefinitionDraftState.AwaitingTarget)
        {
            return;
        }

        var entry = ProposalEntryTestData.CreateEntry(ProposalEntryState.OnMachineDraft);
        draft.Follow(entry, isTargetRegistrationDeleted: false);

        if (state == DefinitionDraftState.Applied)
        {
            Apply(draft);
        }
        else if (state == DefinitionDraftState.Dismissed)
        {
            Dismiss(draft);
        }
    }

    private static void Apply(IDefinitionDraft draft)
    {
        if (draft is MetricDefinitionDraft metric)
        {
            metric.MarkApplied();
        }
        else if (draft is StatusDefinitionDraft status)
        {
            status.MarkApplied();
        }
    }

    private static void Dismiss(IDefinitionDraft draft)
    {
        if (draft is MetricDefinitionDraft metric)
        {
            metric.Dismiss();
        }
        else if (draft is StatusDefinitionDraft status)
        {
            status.Dismiss();
        }
    }

    public static TheoryData<string, IDefinitionDraft> OnMachineDrafts => new()
    {
        {
            "metric",
            MetricDefinitionDraft.ProposeOnMachine(
                ProposalEntryTestData.DraftId,
                ProposalEntryTestData.CompanyId,
                ProposalEntryTestData.AttemptId,
                ProposalEntryTestData.MachineId,
                new MetricDraftValues { Key = "temperature" })
        },
        {
            "status",
            StatusDefinitionDraft.ProposeOnMachine(
                ProposalEntryTestData.DraftId,
                ProposalEntryTestData.CompanyId,
                ProposalEntryTestData.AttemptId,
                ProposalEntryTestData.MachineId,
                new StatusDraftValues { Code = "E1" })
        },
        {
            "phase",
            OperatingPhaseDraft.ProposeOnMachine(
                ProposalEntryTestData.DraftId,
                ProposalEntryTestData.CompanyId,
                ProposalEntryTestData.AttemptId,
                ProposalEntryTestData.MachineId,
                new PhaseDraftValues { Code = "idle" },
                [],
                [])
        },
    };
}
