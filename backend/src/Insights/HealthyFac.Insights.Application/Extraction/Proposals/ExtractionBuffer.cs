using HealthyFac.Insights.Application.Extraction.Signals;
using HealthyFac.Insights.Application.MetricUnits;
using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Insights.Application.Extraction.Proposals;

/// <summary>
/// Collects the proposals of one extraction run in memory. The extractor tools add to it. The mapper builds the
/// message from it after the run ends. A failed or cancelled attempt drops the buffer, so it sends nothing. One
/// instance serves one run, and the tool loop calls the tools one at a time.
/// </summary>
/// <remarks>
/// A machine gets its draft ID when <see cref="AddMachine"/> adds it. The model uses that ID as the
/// <c>machineId</c> of the later tools. A metric key or a status code resolves to the draft of the same run first,
/// else to the definition that the target machine already has.
/// </remarks>
public sealed class ExtractionBuffer
{
    private readonly List<MachineEntry> _machines = [];
    private readonly bool _isMachineFile;
    private readonly Guid? _fileMachineId;
    private readonly IReadOnlyList<MetricUnit> _units;
    private readonly MachineSignals _signals;

    /// <summary>
    /// Creates an empty buffer.
    /// </summary>
    /// <param name="isMachineFile">Tells whether the document belongs to one machine.</param>
    /// <param name="fileMachineId">The machine of the file, or <see langword="null"/> for a company file.</param>
    /// <param name="units">The unit catalog that a metric unit must come from.</param>
    /// <param name="signals">The metric definitions and status definitions of the machine of the file.</param>
    public ExtractionBuffer(
        bool isMachineFile,
        Guid? fileMachineId,
        IReadOnlyList<MetricUnit> units,
        MachineSignals signals
    )
    {
        _isMachineFile = isMachineFile;
        _fileMachineId = fileMachineId;
        _units = units;
        _signals = signals;
    }

    /// <summary>
    /// The machine drafts in the order of the calls. A machine file sends its machine only when the model proposed a
    /// machine value (an update draft). A company file sends every machine that has a value or a definition (a create
    /// draft).
    /// </summary>
    public IReadOnlyList<ProposedMachine> Machines => DraftsProposedFlattening.Machines(Groups, _isMachineFile);

    /// <summary>The metrics with the machine that each belongs to, in the order of the calls.</summary>
    public IReadOnlyList<ProposedMetric> Metrics => DraftsProposedFlattening.Metrics(Groups, _isMachineFile);

    /// <summary>The statuses with the machine that each belongs to, in the order of the calls.</summary>
    public IReadOnlyList<ProposedStatus> Statuses => DraftsProposedFlattening.Statuses(Groups, _isMachineFile);

    /// <summary>The phases with the machine that each belongs to, in the order of the calls.</summary>
    public IReadOnlyList<ProposedPhase> Phases => DraftsProposedFlattening.Phases(Groups, _isMachineFile);

    private IReadOnlyList<ProposedMachineGroup> Groups =>
    [
        .. _machines.Select(machine => new ProposedMachineGroup
        {
            DraftId = machine.DraftId,
            TargetMachineId = _fileMachineId,
            Name = machine.Name,
            SerialNumber = machine.SerialNumber,
            Description = machine.Description,
            Metrics = machine.Metrics,
            Statuses = machine.Statuses,
            Phases = machine.Phases,
        }),
    ];

    /// <summary>
    /// Adds a machine.
    /// </summary>
    /// <param name="name">The name, or <see langword="null"/>.</param>
    /// <param name="serialNumber">The serial number, or <see langword="null"/>.</param>
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    /// <returns>The draft ID of the machine.</returns>
    /// <exception cref="HealthyFac.Shared.Application.ErrorHandling.UnprocessableException">
    /// Thrown by <see cref="ExtractionErrors"/> when a machine file already has its machine, or when the machine
    /// count limit is reached.
    /// </exception>
    public Guid AddMachine(string? name, string? serialNumber, string? description)
    {
        if (_isMachineFile && _machines.Count >= 1)
        {
            throw ExtractionErrors.Rejected(ExtractionErrors.MachineFileHasOneMachine);
        }

        if (_machines.Count >= DraftMessageLimits.MaxMachines)
        {
            throw ExtractionErrors.Rejected(ExtractionErrors.TooManyMachines);
        }

        var entry = new MachineEntry { Name = name, SerialNumber = serialNumber, Description = description };
        _machines.Add(entry);

        return entry.DraftId;
    }

    /// <summary>
    /// Sets the values of a machine that <see cref="AddMachine"/> added without values (a machine that only holds
    /// definitions so far). The values of a machine can be set once.
    /// </summary>
    /// <param name="machineDraftId">The draft ID that <see cref="AddMachine"/> returned.</param>
    /// <param name="name">The name, or <see langword="null"/>.</param>
    /// <param name="serialNumber">The serial number, or <see langword="null"/>.</param>
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    /// <returns>The draft ID of the machine.</returns>
    /// <exception cref="HealthyFac.Shared.Application.ErrorHandling.UnprocessableException">
    /// Thrown by <see cref="ExtractionErrors"/> for an unknown machine, or when the machine has values already.
    /// </exception>
    public Guid UpdateMachine(Guid machineDraftId, string? name, string? serialNumber, string? description)
    {
        var machine = Find(machineDraftId);

        if (machine.HasValues)
        {
            throw ExtractionErrors.Rejected(ExtractionErrors.MachineValuesSetOnce);
        }

        machine.Name = name;
        machine.SerialNumber = serialNumber;
        machine.Description = description;

        return machine.DraftId;
    }

    /// <summary>
    /// Adds a metric to a machine.
    /// </summary>
    /// <param name="machineDraftId">The draft ID that <see cref="AddMachine"/> returned.</param>
    /// <param name="metric">The metric. Its <see cref="ProposedMetric.DraftId"/> is the draft ID that it keeps.</param>
    /// <exception cref="HealthyFac.Shared.Application.ErrorHandling.UnprocessableException">
    /// Thrown by <see cref="ExtractionErrors"/> for an unknown machine, a repeated key, or a reached count limit.
    /// </exception>
    public void AddMetric(Guid machineDraftId, ProposedMetric metric)
    {
        var machine = Find(machineDraftId);

        if (machine.Metrics.Count >= DraftMessageLimits.MaxMetrics)
        {
            throw ExtractionErrors.Rejected(ExtractionErrors.TooManyMetrics);
        }

        if (metric.Key is { } key && machine.Metrics.Any(other => SameText(other.Key, key)))
        {
            throw ExtractionErrors.DuplicateMetricKey(key);
        }

        machine.Metrics.Add(metric);
    }

    /// <summary>
    /// Adds a status to a machine.
    /// </summary>
    /// <param name="machineDraftId">The draft ID that <see cref="AddMachine"/> returned.</param>
    /// <param name="status">The status.</param>
    /// <exception cref="HealthyFac.Shared.Application.ErrorHandling.UnprocessableException">
    /// Thrown by <see cref="ExtractionErrors"/> for an unknown machine, a repeated code, or a reached count limit.
    /// </exception>
    public void AddStatus(Guid machineDraftId, ProposedStatus status)
    {
        var machine = Find(machineDraftId);

        if (machine.Statuses.Count >= DraftMessageLimits.MaxStatuses)
        {
            throw ExtractionErrors.Rejected(ExtractionErrors.TooManyStatuses);
        }

        if (status.Code is { } code && machine.Statuses.Any(other => SameText(other.Code, code)))
        {
            throw ExtractionErrors.DuplicateStatusCode(code);
        }

        machine.Statuses.Add(status);
    }

    /// <summary>
    /// Adds a phase to a machine.
    /// </summary>
    /// <param name="machineDraftId">The draft ID that <see cref="AddMachine"/> returned.</param>
    /// <param name="phase">The phase with its resolved conditions and limit rows.</param>
    /// <exception cref="HealthyFac.Shared.Application.ErrorHandling.UnprocessableException">
    /// Thrown by <see cref="ExtractionErrors"/> for an unknown machine or a repeated code.
    /// </exception>
    public void AddPhase(Guid machineDraftId, ProposedPhase phase)
    {
        var machine = Find(machineDraftId);

        if (phase.Code is { } code && machine.Phases.Any(other => SameText(other.Code, code)))
        {
            throw ExtractionErrors.DuplicatePhaseCode(code);
        }

        machine.Phases.Add(phase);
    }

    /// <summary>
    /// Finds a unit of the catalog by its pair. A discrete unit never matches.
    /// </summary>
    /// <param name="quantityKind">The quantity kind.</param>
    /// <param name="unit">The unit name.</param>
    /// <returns>The unit, or <see langword="null"/> when the catalog has no such pair.</returns>
    public MetricUnit? FindUnit(string quantityKind, string unit) =>
        _units.FirstOrDefault(candidate => !candidate.IsDiscrete
            && SameText(candidate.QuantityKind, quantityKind)
            && SameText(candidate.Name, unit));

    /// <summary>
    /// Resolves a metric key of a machine to the draft of this run, else to the definition of the machine.
    /// </summary>
    /// <param name="machineDraftId">The draft ID that <see cref="AddMachine"/> returned.</param>
    /// <param name="key">The metric key.</param>
    /// <returns>The draft ID or the definition ID of the metric.</returns>
    /// <exception cref="HealthyFac.Shared.Application.ErrorHandling.UnprocessableException">
    /// Thrown by <see cref="ExtractionErrors"/> for an unknown machine or a key that nothing names.
    /// </exception>
    public ResolvedSignal ResolveMetric(Guid machineDraftId, string key)
    {
        var machine = Find(machineDraftId);
        var draft = machine.Metrics.FirstOrDefault(metric => SameText(metric.Key, key));

        if (draft is not null)
        {
            return new ResolvedSignal { DraftId = draft.DraftId };
        }

        var definition = _isMachineFile
            ? _signals.Metrics.FirstOrDefault(metric => SameText(metric.Key, key))
            : null;

        return definition is null
            ? throw ExtractionErrors.UnknownMetricKey(key)
            : new ResolvedSignal { DefinitionId = definition.Id };
    }

    /// <summary>
    /// Resolves a status code of a machine to the draft of this run, else to the definition of the machine.
    /// </summary>
    /// <param name="machineDraftId">The draft ID that <see cref="AddMachine"/> returned.</param>
    /// <param name="code">The status code.</param>
    /// <returns>The draft ID or the definition ID of the status.</returns>
    /// <exception cref="HealthyFac.Shared.Application.ErrorHandling.UnprocessableException">
    /// Thrown by <see cref="ExtractionErrors"/> for an unknown machine or a code that nothing names.
    /// </exception>
    public ResolvedSignal ResolveStatus(Guid machineDraftId, string code)
    {
        var machine = Find(machineDraftId);
        var draft = machine.Statuses.FirstOrDefault(status => SameText(status.Code, code));

        if (draft is not null)
        {
            return new ResolvedSignal { DraftId = draft.DraftId };
        }

        var definition = _isMachineFile
            ? _signals.Statuses.FirstOrDefault(status => SameText(status.Key, code))
            : null;

        return definition is null
            ? throw ExtractionErrors.UnknownStatusCode(code)
            : new ResolvedSignal { DefinitionId = definition.Id };
    }

    private static bool SameText(string? left, string? right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private MachineEntry Find(Guid machineDraftId) =>
        _machines.FirstOrDefault(machine => machine.DraftId == machineDraftId)
        ?? throw ExtractionErrors.UnknownMachine();

    private sealed class MachineEntry
    {
        public Guid DraftId { get; } = Guid.CreateVersion7();

        public string? Name { get; set; }

        public string? SerialNumber { get; set; }

        public string? Description { get; set; }

        public bool HasValues => Name is not null || SerialNumber is not null || Description is not null;

        public List<ProposedMetric> Metrics { get; } = [];

        public List<ProposedStatus> Statuses { get; } = [];

        public List<ProposedPhase> Phases { get; } = [];
    }
}

/// <summary>
/// The result of a key resolution: the draft of this run or the definition that the machine already has.
/// </summary>
public sealed record ResolvedSignal
{
    public Guid? DraftId { get; init; }

    public Guid? DefinitionId { get; init; }
}
