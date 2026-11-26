using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;

namespace HealthyFac.Telemetry.UnitTests.DefinitionDrafts.Data;

public static class ProposalEntryTestData
{
    public static readonly Guid CompanyId = Guid.Parse(input: "0f1a2b3c-0000-7000-8000-000000000001");
    public static readonly Guid EntryId = Guid.Parse(input: "0f1a2b3c-0000-7000-8000-000000000002");
    public static readonly Guid MachineId = Guid.Parse(input: "0f1a2b3c-0000-7000-8000-000000000003");
    public static readonly Guid OtherMachineId = Guid.Parse(input: "0f1a2b3c-0000-7000-8000-000000000004");
    public static readonly Guid AttemptId = Guid.Parse(input: "0f1a2b3c-0000-7000-8000-000000000005");
    public static readonly Guid FileId = Guid.Parse(input: "0f1a2b3c-0000-7000-8000-000000000006");
    public static readonly Guid DraftId = Guid.Parse(input: "0f1a2b3c-0000-7000-8000-000000000007");

    private static readonly ProposalEntryState[] States =
    [
        ProposalEntryState.Awaiting,
        ProposalEntryState.Rejected,
        ProposalEntryState.OnMachineDraft,
        ProposalEntryState.OnMachine,
        ProposalEntryState.MachineDraftDismissed,
    ];

    private static readonly (string Name, Func<ProposalEntry, bool> Move, ProposalEntryState[] Allowed,
        ProposalEntryState Target)[] Moves =
        [
            ("Reject", entry => entry.Reject(), [ProposalEntryState.Awaiting], ProposalEntryState.Rejected),
            (
                "AcceptOnMachine",
                entry => entry.AcceptOnMachine(MachineId),
                [ProposalEntryState.Awaiting],
                ProposalEntryState.OnMachine
            ),
            (
                "AcceptOnMachineDraft",
                entry => entry.AcceptOnMachineDraft(),
                [ProposalEntryState.Awaiting],
                ProposalEntryState.OnMachineDraft
            ),
            (
                "ApplyMachineDraft",
                entry => entry.ApplyMachineDraft(MachineId),
                [ProposalEntryState.Awaiting, ProposalEntryState.OnMachineDraft],
                ProposalEntryState.OnMachine
            ),
            (
                "DismissMachineDraft",
                entry => entry.DismissMachineDraft(),
                [ProposalEntryState.Awaiting, ProposalEntryState.OnMachineDraft],
                ProposalEntryState.MachineDraftDismissed
            ),
        ];

    public static TheoryData<string, ProposalEntryState, Func<ProposalEntry, bool>, bool, ProposalEntryState>
        Transitions
    {
        get
        {
            var data = new TheoryData<string, ProposalEntryState, Func<ProposalEntry, bool>, bool,
                ProposalEntryState>();

            foreach (var start in States)
            {
                foreach (var (name, move, allowed, target) in Moves)
                {
                    var isAllowed = allowed.Contains(start);

                    data.Add(
                        $"{name} from {start.Value}",
                        start,
                        move,
                        isAllowed,
                        isAllowed ? target : start);
                }
            }

            return data;
        }
    }

    public static ProposalEntry CreateEntry(ProposalEntryState state)
    {
        var entry = new ProposalEntry(EntryId, CompanyId);

        if (state == ProposalEntryState.Rejected)
        {
            entry.Reject();
        }
        else if (state == ProposalEntryState.OnMachineDraft)
        {
            entry.AcceptOnMachineDraft();
        }
        else if (state == ProposalEntryState.OnMachine)
        {
            entry.AcceptOnMachine(MachineId);
        }
        else if (state == ProposalEntryState.MachineDraftDismissed)
        {
            entry.DismissMachineDraft();
        }

        return entry;
    }
}
