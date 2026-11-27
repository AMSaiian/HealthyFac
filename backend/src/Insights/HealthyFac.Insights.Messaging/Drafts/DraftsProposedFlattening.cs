namespace HealthyFac.Insights.Messaging.Drafts;

/// <summary>
/// The one rule that turns the machines with their definitions into the flat lists of <see cref="DraftsProposed"/>.
/// The extraction buffer and the seeder call it, so a message of either has the same shape.
/// </summary>
/// <remarks>
/// A machine file sends its machine only when it has a value (an update draft), and each definition names the target
/// machine with <c>MachineId</c>. A company file sends a machine that has a value or at least 1 definition (a create
/// draft), and each definition names its machine draft with <c>MachineDraftId</c>.
/// </remarks>
public static class DraftsProposedFlattening
{
    /// <summary>Builds the machine drafts of the message.</summary>
    /// <param name="groups">The machines with their definitions, in the order of the proposal.</param>
    /// <param name="isMachineFile">Tells whether the document belongs to one machine.</param>
    /// <returns>The machine drafts that the message sends.</returns>
    public static IReadOnlyList<ProposedMachine> Machines(
        IReadOnlyList<ProposedMachineGroup> groups,
        bool isMachineFile
    ) =>
    [
        .. groups
            .Where(group => HasValue(group) || (!isMachineFile && HasDefinitions(group)))
            .Select(group => new ProposedMachine
            {
                DraftId = group.DraftId,
                TargetMachineId = isMachineFile ? group.TargetMachineId : null,
                Name = group.Name,
                SerialNumber = group.SerialNumber,
                Description = group.Description,
            }),
    ];

    /// <summary>Builds the metrics of the message, each with the machine that it belongs to.</summary>
    /// <param name="groups">The machines with their definitions.</param>
    /// <param name="isMachineFile">Tells whether the document belongs to one machine.</param>
    /// <returns>The metrics in the order of the groups.</returns>
    public static IReadOnlyList<ProposedMetric> Metrics(
        IReadOnlyList<ProposedMachineGroup> groups,
        bool isMachineFile
    ) =>
    [
        .. groups.SelectMany(group => group.Metrics.Select(metric => metric with
        {
            MachineId = MachineIdOf(group, isMachineFile),
            MachineDraftId = MachineDraftIdOf(group, isMachineFile),
        })),
    ];

    /// <summary>Builds the statuses of the message, each with the machine that it belongs to.</summary>
    /// <param name="groups">The machines with their definitions.</param>
    /// <param name="isMachineFile">Tells whether the document belongs to one machine.</param>
    /// <returns>The statuses in the order of the groups.</returns>
    public static IReadOnlyList<ProposedStatus> Statuses(
        IReadOnlyList<ProposedMachineGroup> groups,
        bool isMachineFile
    ) =>
    [
        .. groups.SelectMany(group => group.Statuses.Select(status => status with
        {
            MachineId = MachineIdOf(group, isMachineFile),
            MachineDraftId = MachineDraftIdOf(group, isMachineFile),
        })),
    ];

    /// <summary>Builds the phases of the message, each with the machine that it belongs to.</summary>
    /// <param name="groups">The machines with their definitions.</param>
    /// <param name="isMachineFile">Tells whether the document belongs to one machine.</param>
    /// <returns>The phases in the order of the groups.</returns>
    public static IReadOnlyList<ProposedPhase> Phases(IReadOnlyList<ProposedMachineGroup> groups, bool isMachineFile) =>
    [
        .. groups.SelectMany(group => group.Phases.Select(phase => phase with
        {
            MachineId = MachineIdOf(group, isMachineFile),
            MachineDraftId = MachineDraftIdOf(group, isMachineFile),
        })),
    ];

    private static bool HasValue(ProposedMachineGroup group) =>
        group.Name is not null || group.SerialNumber is not null || group.Description is not null;

    private static bool HasDefinitions(ProposedMachineGroup group) =>
        group.Metrics.Count > 0 || group.Statuses.Count > 0 || group.Phases.Count > 0;

    private static Guid? MachineIdOf(ProposedMachineGroup group, bool isMachineFile) =>
        isMachineFile ? group.TargetMachineId : null;

    private static Guid? MachineDraftIdOf(ProposedMachineGroup group, bool isMachineFile) =>
        isMachineFile ? null : group.DraftId;
}
