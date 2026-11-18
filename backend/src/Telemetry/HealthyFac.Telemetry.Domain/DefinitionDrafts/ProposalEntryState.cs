using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Telemetry.Domain.DefinitionDrafts;

/// <summary>
/// Defines the states of one proposed machine of a proposal. The camelCase values reach the API and the database,
/// so never rename one in place.
/// </summary>
public sealed class ProposalEntryState : Enumeration<ProposalEntryState>
{
    /// <summary>No message told the outcome of the machine check yet.</summary>
    public static readonly ProposalEntryState Awaiting = new(value: "awaiting");

    /// <summary>Core rejected the proposal.</summary>
    public static readonly ProposalEntryState Rejected = new(value: "rejected");

    /// <summary>Core stored a create machine draft for the entry.</summary>
    public static readonly ProposalEntryState OnMachineDraft = new(value: "onMachineDraft");

    /// <summary>The entry belongs to a machine.</summary>
    public static readonly ProposalEntryState OnMachine = new(value: "onMachine");

    /// <summary>A user dismissed the create machine draft of the entry.</summary>
    public static readonly ProposalEntryState MachineDraftDismissed = new(value: "machineDraftDismissed");

    private ProposalEntryState(string value)
        : base(value)
    {
    }
}
