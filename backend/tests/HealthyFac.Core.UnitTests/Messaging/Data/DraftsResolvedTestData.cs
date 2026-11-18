using HealthyFac.Core.Messaging.Drafts;

namespace HealthyFac.Core.UnitTests.Messaging.Data;

public static class DraftsResolvedTestData
{
    private static readonly Guid Id = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000d01");

    /// <summary>The case name, the resolution, the entry, and whether the message is valid.</summary>
    public static TheoryData<string, DraftResolution, ResolvedEntry, bool> Entries => new()
    {
        { "AcceptedOnMachine", DraftResolution.Accepted, new ResolvedEntry { EntryId = Id, MachineId = Id }, true },
        {
            "AcceptedOnMachineDraft",
            DraftResolution.Accepted,
            new ResolvedEntry { EntryId = Id, IsOnMachineDraft = true },
            true
        },
        {
            "AcceptedWithBoth",
            DraftResolution.Accepted,
            new ResolvedEntry { EntryId = Id, MachineId = Id, IsOnMachineDraft = true },
            false
        },
        { "AcceptedWithNeither", DraftResolution.Accepted, new ResolvedEntry { EntryId = Id }, false },
        { "RejectedWithNeither", DraftResolution.Rejected, new ResolvedEntry { EntryId = Id }, true },
        { "RejectedWithMachine", DraftResolution.Rejected, new ResolvedEntry { EntryId = Id, MachineId = Id }, false },
        {
            "RejectedWithMachineDraft",
            DraftResolution.Rejected,
            new ResolvedEntry { EntryId = Id, IsOnMachineDraft = true },
            false
        },
    };
}
