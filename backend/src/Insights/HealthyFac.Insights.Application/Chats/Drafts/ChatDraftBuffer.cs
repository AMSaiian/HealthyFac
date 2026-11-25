using HealthyFac.Insights.Application.Extraction.Proposals;
using HealthyFac.Insights.Application.Extraction.Signals;
using HealthyFac.Insights.Application.MetricUnits;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Insights.Application.Chats.Drafts;

/// <summary>
/// Collects the proposals of one chat turn in memory. The propose tools add to it, and the final save of a completed
/// turn sends them. One instance serves the request scope of one turn, and the tool loop calls the tools one at a
/// time.
/// </summary>
/// <remarks>
/// The buffer reuses <see cref="ExtractionBuffer"/> for the repeat rules, the count rules, and the key resolution of
/// the extraction. One buffer in the mode of a company file holds the create drafts of the turn. Each existing
/// machine that the model proposes for gets one more buffer in the mode of a machine file, so its definitions map to
/// <c>MachineId</c>. A create draft maps to <c>MachineDraftId</c>. The model never sees an entry: it sees the draft ID
/// of a create draft, or the ID of an existing machine.
/// </remarks>
public sealed class ChatDraftBuffer
{
    private const string MachineTitle = "Machine";
    private const string MetricTitle = "Metric";
    private const string StatusTitle = "Status";
    private const string PhaseTitle = "Operating phase";
    private const string WorkOrderTitle = "Work order";

    private readonly Dictionary<Guid, ExistingMachine> _existing = [];
    private readonly HashSet<Guid> _createDraftIds = [];
    private readonly List<ChatWorkOrderProposal> _workOrders = [];

    private ExtractionBuffer? _created;

    /// <summary>The work orders in the order of the calls.</summary>
    public IReadOnlyList<ChatWorkOrderProposal> WorkOrders => _workOrders;

    /// <summary>The machine drafts of the turn: the create drafts, then the update drafts.</summary>
    public IReadOnlyList<ProposedMachine> Machines =>
        [.. Buffers.SelectMany(buffer => buffer.Machines)];

    /// <summary>The metric drafts of the turn.</summary>
    public IReadOnlyList<ProposedMetric> Metrics => [.. Buffers.SelectMany(buffer => buffer.Metrics)];

    /// <summary>The status drafts of the turn.</summary>
    public IReadOnlyList<ProposedStatus> Statuses => [.. Buffers.SelectMany(buffer => buffer.Statuses)];

    /// <summary>The phase drafts of the turn.</summary>
    public IReadOnlyList<ProposedPhase> Phases => [.. Buffers.SelectMany(buffer => buffer.Phases)];

    /// <summary>Tells whether the turn proposed nothing.</summary>
    public bool IsEmpty =>
        Machines.Count + Metrics.Count + Statuses.Count + Phases.Count + _workOrders.Count == 0;

    /// <summary>
    /// The draft links of the reply: one row for each proposal, in the order machine, metric, status, phase, and work
    /// order.
    /// </summary>
    public IReadOnlyList<ChatMessageDraft> DraftRows =>
    [
        .. Machines.Select(machine => new ChatMessageDraft(
            ChatDraftKind.Machine,
            machine.DraftId,
            Title(machine.Name ?? machine.SerialNumber, MachineTitle),
            machine.TargetMachineId)),
        .. Metrics.Select(metric => new ChatMessageDraft(
            ChatDraftKind.Metric,
            metric.DraftId,
            Title(metric.Name ?? metric.Key, MetricTitle),
            metric.MachineId)),
        .. Statuses.Select(status => new ChatMessageDraft(
            ChatDraftKind.Status,
            status.DraftId,
            Title(status.Code, StatusTitle),
            status.MachineId)),
        .. Phases.Select(phase => new ChatMessageDraft(
            ChatDraftKind.OperatingPhase,
            phase.DraftId,
            Title(phase.Name ?? phase.Code, PhaseTitle),
            phase.MachineId)),
        .. _workOrders.Select(workOrder => new ChatMessageDraft(
            ChatDraftKind.WorkOrder,
            workOrder.DraftId,
            Title(workOrder.Title, WorkOrderTitle),
            workOrder.MachineId)),
    ];

    private static string Title(string? text, string fallback) =>
        string.IsNullOrWhiteSpace(text) ? fallback : text;

    private IEnumerable<ExtractionBuffer> Buffers =>
        _created is null
            ? _existing.Values.Select(machine => machine.Buffer)
            : [_created, .. _existing.Values.Select(machine => machine.Buffer)];

    /// <summary>
    /// Tells whether the ID is a create draft that <see cref="RegisterCreateDraft"/> stored in this turn.
    /// </summary>
    /// <param name="machineId">The ID that the model gave.</param>
    /// <returns><see langword="true"/> for a create draft of this turn.</returns>
    public bool IsCreateDraft(Guid machineId) => _createDraftIds.Contains(machineId);

    /// <summary>
    /// Gets the buffer of the create drafts. The first call creates it.
    /// </summary>
    /// <param name="units">The unit catalog that a metric unit must come from.</param>
    /// <returns>The buffer in the mode of a company file.</returns>
    public ExtractionBuffer GetCreateBuffer(IReadOnlyList<MetricUnit> units) =>
        _created ??= new ExtractionBuffer(
            isMachineFile: false,
            fileMachineId: null,
            units,
            new MachineSignals { Metrics = [], Statuses = [] });

    /// <summary>
    /// Remembers the ID of a create draft that the model may use as the <c>machineId</c> of later tools.
    /// </summary>
    /// <param name="draftId">The draft ID that the buffer of the create drafts returned.</param>
    public void RegisterCreateDraft(Guid draftId) => _createDraftIds.Add(draftId);

    /// <summary>
    /// Finds the buffer of an existing machine.
    /// </summary>
    /// <param name="machineId">The ID of the existing machine.</param>
    /// <returns>The entry, or <see langword="null"/> when the turn did not touch the machine.</returns>
    public ExistingMachine? FindExisting(Guid machineId) => _existing.GetValueOrDefault(machineId);

    /// <summary>
    /// Adds the buffer of an existing machine.
    /// </summary>
    /// <param name="machineId">The ID of the existing machine.</param>
    /// <param name="units">The unit catalog that a metric unit must come from.</param>
    /// <param name="signals">The metric and status definitions that the machine already has.</param>
    /// <returns>The new entry.</returns>
    public ExistingMachine AddExisting(Guid machineId, IReadOnlyList<MetricUnit> units, MachineSignals signals)
    {
        var machine = new ExistingMachine(new ExtractionBuffer(isMachineFile: true, machineId, units, signals));
        _existing[machineId] = machine;

        return machine;
    }

    /// <summary>
    /// Adds a work order.
    /// </summary>
    /// <param name="workOrder">The work order with its draft ID.</param>
    public void AddWorkOrder(ChatWorkOrderProposal workOrder) => _workOrders.Add(workOrder);

    /// <summary>
    /// The buffer of one existing machine. The container is the machine entry of the buffer: it carries the update
    /// values of the machine, and the definitions of the machine hang on it.
    /// </summary>
    public sealed class ExistingMachine(ExtractionBuffer buffer)
    {
        public ExtractionBuffer Buffer { get; } = buffer;

        /// <summary>The draft ID of the machine entry, or <see langword="null"/> before the first call.</summary>
        public Guid? ContainerId { get; set; }
    }
}
