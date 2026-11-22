using HealthyFac.Core.Application.Drafts;
using HealthyFac.Core.Application.Machines.States;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Seeder.Ingest;
using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.People;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Telemetry.Application.DefinitionDrafts;
using HealthyFac.Telemetry.Application.MetricDefinitions;
using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using HealthyFac.Telemetry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.Machines;

/// <summary>
/// Seeds the machines of ingest set 1 and their definitions from <c>machines.json</c>. A person applies the machine
/// draft, and 30 minutes later the metric, status, and phase drafts of the machine, as the review screens of the
/// product do. Every step goes through the use cases, so the relay moves the drafts and ties the file. The same file
/// holds the state changes of the machines.
/// </summary>
public sealed class MachinesPart : ISeedPart
{
    private readonly ServiceHosts.ServiceHosts _hosts;
    private readonly Dictionary<string, Func<SeedContext, CancellationToken, Task>> _actions = [];

    private MachinesFile _file = null!;
    private IReadOnlyDictionary<Guid, ProposedMachine> _drafts = null!;

    public MachinesPart(ServiceHosts.ServiceHosts hosts)
    {
        _hosts = hosts;
    }

    public IReadOnlyList<SeedEvent> ReadEvents(SeedFolder folder)
    {
        _file = folder.Read<MachinesFile>(MachinesConstants.FileName);
        _drafts = folder.Read<IngestSetFile>(MachinesConstants.SetFileName)
            .Entries
            .SelectMany(entry => entry.DraftsProposed.Machines)
            .ToDictionary(machine => machine.DraftId);
        _actions.Clear();
        var events = new List<SeedEvent>();
        foreach (var machine in _file.Machines)
        {
            events.Add(ApplyEvent(machine));
            events.Add(DefinitionsEvent(machine));
        }

        events.AddRange(_file.StateChanges.Select(StateEvent));

        return events;
    }

    public Task RunAsync(SeedEvent seedEvent, SeedContext context, CancellationToken cancellationToken) =>
        _actions[SeedItem.KeyOf(seedEvent.Item)](context, cancellationToken);

    private static string MachineKey(string key) => $"{key}{MachinesConstants.MachineSuffix}";

    private static string UserKey(string person) => $"{person}{PeopleConstants.UserSuffix}";

    private static string Required(string? value, string item, string member) =>
        value ?? throw new SeedFileException(MachinesErrors.DraftIncomplete(item, member));

    private static SeedRequest Request(ServiceName service, object request) =>
        new() { Service = service, Request = request };

    private SeedEvent ApplyEvent(MachineSeed machine)
    {
        var key = $"{machine.Key}{MachinesConstants.AppliedSuffix}";
        var item = SeedItem.Of(MachinesConstants.FileName, key);
        _actions[key] = (context, cancellationToken) => ApplyAsync(machine, item, context, cancellationToken);

        return new SeedEvent
        {
            At = machine.AppliedAt,
            Actor = machine.AppliedBy,
            Item = item,
            Creates = [MachineKey(machine.Key)],
            Uses = [PeopleConstants.CompanyKey, UserKey(machine.AppliedBy)],
            Requests =
            [
                Request(ServiceName.Core, ApplyRequest(machine, item, PeopleConstants.CheckId, DateOnly.MinValue)),
            ],
        };
    }

    private SeedEvent DefinitionsEvent(MachineSeed machine)
    {
        var key = $"{machine.Key}{MachinesConstants.DefinitionsSuffix}";
        var item = SeedItem.Of(MachinesConstants.FileName, key);
        _actions[key] = (context, cancellationToken) =>
            ApplyDefinitionsAsync(machine, item, context, cancellationToken);
        var draft = FindDraft(machine, item);
        Func<string, Guid> checkId = _ => PeopleConstants.CheckId;

        return new SeedEvent
        {
            At = DefinitionsAt(machine, item),
            Actor = machine.AppliedBy,
            Item = item,
            Creates = [item],
            Uses = [MachineKey(machine.Key), UserKey(machine.AppliedBy)],
            Requests =
            [
                Request(
                    ServiceName.Telemetry,
                    MetricsRequest(draft, item, PeopleConstants.CheckId, PeopleConstants.CheckId, expectedRevision: 0)),
                Request(
                    ServiceName.Telemetry,
                    StatusesRequest(draft, item, PeopleConstants.CheckId, PeopleConstants.CheckId)),
                .. draft.Phases.Select(phase => Request(
                    ServiceName.Telemetry,
                    PhaseRequest(
                        draft, phase, item, PeopleConstants.CheckId, PeopleConstants.CheckId, checkId, checkId))),
            ],
        };
    }

    private SeedEvent StateEvent(MachineStateSeed change)
    {
        var key = $"{change.Machine}{MachinesConstants.StateSuffix}.{change.At}";
        var item = SeedItem.Of(MachinesConstants.FileName, key);
        _actions[key] = (context, cancellationToken) => ChangeStateAsync(change, context, cancellationToken);

        return new SeedEvent
        {
            At = change.At,
            Actor = change.ChangedBy,
            Item = item,
            Uses = [MachineKey(change.Machine), UserKey(change.ChangedBy)],
            Requests =
            [
                Request(
                    ServiceName.Core,
                    new ChangeMachineStateRequest
                    {
                        CompanyId = PeopleConstants.CheckId,
                        Id = PeopleConstants.CheckId,
                        State = change.State,
                    }),
            ],
        };
    }

    private RelativeMoment DefinitionsAt(MachineSeed machine, string item)
    {
        if (machine.AppliedAt.Time is not { } time)
        {
            throw new SeedFileException(MachinesErrors.AppliedNeedsTime(item));
        }

        if (time.ToTimeSpan() + MachinesConstants.DefinitionsDelay >= TimeSpan.FromDays(days: 1))
        {
            throw new SeedFileException(MachinesErrors.AppliedTooLate(item));
        }

        return machine.AppliedAt with { Time = time.Add(MachinesConstants.DefinitionsDelay) };
    }

    private ProposedMachine FindDraft(MachineSeed machine, string item) =>
        _drafts.TryGetValue(machine.DraftId, out var draft)
            ? draft
            : throw new SeedFileException(MachinesErrors.DraftUnknown(item, machine.DraftId));

    private ApplyMachineDraftRequest ApplyRequest(MachineSeed machine, string item, Guid companyId, DateOnly goLiveOn)
    {
        var draft = FindDraft(machine, item);

        return new ApplyMachineDraftRequest
        {
            CompanyId = companyId,
            Id = draft.DraftId,
            Name = Required(draft.Name, item, nameof(draft.Name)),
            SerialNumber = Required(draft.SerialNumber, item, nameof(draft.SerialNumber)),
            Description = draft.Description,
            ExpectedGoLiveOn = goLiveOn,
        };
    }

    private async Task ApplyAsync(
        MachineSeed machine,
        string item,
        SeedContext context,
        CancellationToken cancellationToken
    )
    {
        var goLiveOn = SeedCalendar.ToDate(machine.ExpectedGoLiveOn, context.Anchor, context.Zone);
        var response = await _hosts.Core.SendAsync<ApplyMachineDraftRequest, ApplyMachineDraftResponse>(
            context.GetActor(machine.AppliedBy),
            ApplyRequest(machine, item, context.GetId(PeopleConstants.CompanyKey), goLiveOn),
            cancellationToken);
        context.SetId(MachineKey(machine.Key), response.MachineId);
    }

    private async Task ApplyDefinitionsAsync(
        MachineSeed machine,
        string item,
        SeedContext context,
        CancellationToken cancellationToken
    )
    {
        var actor = context.GetActor(machine.AppliedBy);
        var companyId = context.GetId(PeopleConstants.CompanyKey);
        var machineId = context.GetId(MachineKey(machine.Key));
        var draft = FindDraft(machine, item);
        var revision = await ReadRevisionAsync(machineId, cancellationToken);

        await _hosts.Telemetry.SendAsync<ApplyMetricDefinitionChangesRequest, ApplyMetricDefinitionChangesResponse>(
            actor,
            MetricsRequest(draft, item, companyId, machineId, revision),
            cancellationToken);
        await _hosts.Telemetry.SendAsync<ApplyStatusDefinitionChangesRequest, ApplyStatusDefinitionChangesResponse>(
            actor,
            StatusesRequest(draft, item, companyId, machineId),
            cancellationToken);
        var (metricIds, statusIds) = await ReadDefinitionIdsAsync(machineId, cancellationToken);
        foreach (var phase in draft.Phases)
        {
            await _hosts.Telemetry.SendAsync<ApplyOperatingPhaseDraftRequest, ApplyOperatingPhaseDraftResponse>(
                actor,
                PhaseRequest(
                    draft,
                    phase,
                    item,
                    companyId,
                    machineId,
                    key => FindId(metricIds, key),
                    code => FindId(statusIds, code)),
                cancellationToken);
        }

        context.SetId(item, machineId);
    }

    private async Task ChangeStateAsync(
        MachineStateSeed change,
        SeedContext context,
        CancellationToken cancellationToken
    )
    {
        await _hosts.Core.SendAsync(
            context.GetActor(change.ChangedBy),
            new ChangeMachineStateRequest
            {
                CompanyId = context.GetId(PeopleConstants.CompanyKey),
                Id = context.GetId(MachineKey(change.Machine)),
                State = change.State,
            },
            cancellationToken);
    }

    private async Task<int> ReadRevisionAsync(Guid machineId, CancellationToken cancellationToken)
    {
        using var scope = _hosts.Telemetry.CreateScope(ServiceActor.DatabaseInitialization.For(ServiceName.Telemetry));

        return await scope.ServiceProvider.GetRequiredService<TelemetryDbContext>()
            .MachineRegistrations
            .Where(registration => registration.Id == machineId)
            .Select(registration => registration.MetricSchemaRevision)
            .SingleAsync(cancellationToken);
    }

    private async Task<(Dictionary<string, Guid> Metrics, Dictionary<string, Guid> Statuses)> ReadDefinitionIdsAsync(
        Guid machineId,
        CancellationToken cancellationToken
    )
    {
        using var scope = _hosts.Telemetry.CreateScope(ServiceActor.DatabaseInitialization.For(ServiceName.Telemetry));
        var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
        var metrics = await dbContext.MetricDefinitions
            .Where(definition => definition.MachineRegistrationId == machineId)
            .ToDictionaryAsync(definition => definition.Key, definition => definition.Id, cancellationToken);
        var statuses = await dbContext.StatusDefinitions
            .Where(definition => definition.MachineRegistrationId == machineId)
            .ToDictionaryAsync(definition => definition.Code, definition => definition.Id, cancellationToken);

        return (metrics, statuses);
    }

    private static Guid FindId(IReadOnlyDictionary<string, Guid> ids, string name) =>
        ids.TryGetValue(name, out var id)
            ? id
            : throw new InvalidOperationException(MachinesErrors.DefinitionMissing(name));

    private static ApplyMetricDefinitionChangesRequest MetricsRequest(
        ProposedMachine draft,
        string item,
        Guid companyId,
        Guid machineId,
        int expectedRevision
    ) =>
        new()
        {
            CompanyId = companyId,
            MachineId = machineId,
            ExpectedRevision = expectedRevision,
            Created = draft.Metrics.Select(metric => CreatedMetric(metric, item)).ToList(),
        };

    private static CreatedMetricDefinition CreatedMetric(ProposedMetric metric, string item) =>
        new()
        {
            DraftId = metric.DraftId,
            Key = Required(metric.Key, item, nameof(metric.Key)),
            Name = Required(metric.Name, item, nameof(metric.Name)),
            Description = metric.Description,
            ValueKind = metric.ValueKind is null ? null : MetricValueKind.FromValue(metric.ValueKind),
            Unit = metric is { QuantityKind: { } quantityKind, Unit: { } unit }
                ? new MetricUnitReference { QuantityKind = quantityKind, Name = unit }
                : null,
            Min = metric.Min,
            Max = metric.Max,
            States = metric.States,
            WarningLow = metric.WarningLow,
            WarningHigh = metric.WarningHigh,
            AlarmLow = metric.AlarmLow,
            AlarmHigh = metric.AlarmHigh,
            Behavior = metric.Behavior is null ? null : MetricBehavior.FromValue(metric.Behavior),
        };

    private static ApplyStatusDefinitionChangesRequest StatusesRequest(
        ProposedMachine draft,
        string item,
        Guid companyId,
        Guid machineId
    ) =>
        new()
        {
            CompanyId = companyId,
            MachineId = machineId,
            Items = draft.Statuses
                .Select(status => new StatusDefinitionChange
                {
                    DraftId = status.DraftId,
                    Code = Required(status.Code, item, nameof(status.Code)),
                    Severity = status.Severity is null ? null : StatusSeverity.FromValue(status.Severity),
                    Description = status.Description,
                })
                .ToList(),
        };

    private static ApplyOperatingPhaseDraftRequest PhaseRequest(
        ProposedMachine draft,
        ProposedPhase phase,
        string item,
        Guid companyId,
        Guid machineId,
        Func<string, Guid> metricId,
        Func<string, Guid> statusId
    )
    {
        Guid MetricOf(Guid draftId)
        {
            return metricId(
                Required(
                    draft.Metrics.Single(metric => metric.DraftId == draftId).Key, item, nameof(ProposedMetric.Key)));
        }

        Guid StatusOf(Guid draftId)
        {
            return statusId(
                Required(
                    draft.Statuses.Single(status => status.DraftId == draftId).Code, item,
                    nameof(ProposedStatus.Code)));
        }

        return new ApplyOperatingPhaseDraftRequest
        {
            CompanyId = companyId,
            MachineId = machineId,
            Id = phase.DraftId,
            Code = Required(phase.Code, item, nameof(phase.Code)),
            Name = Required(phase.Name, item, nameof(phase.Name)),
            Description = phase.Description,
            Priority = phase.Priority ?? 0,
            IsTracked = phase.IsTracked ?? true,
            Conditions = phase.Conditions
                .Select(condition => new PhaseConditionInput
                {
                    MetricDefinitionId = condition.MetricDraftId is { } metricDraftId ? MetricOf(metricDraftId) : null,
                    StatusDefinitionId = condition.StatusDraftId is { } statusDraftId ? StatusOf(statusDraftId) : null,
                    Operator = condition.Operator is null ? null : PhaseConditionOperator.FromValue(condition.Operator),
                    Value = condition.Value,
                    UpperValue = condition.UpperValue,
                    States = condition.States,
                })
                .ToList(),
            Limits = phase.Limits
                .Select(limit => new PhaseLimitInput
                {
                    MetricDefinitionId = MetricOf(limit.MetricDraftId!.Value),
                    WarningLow = limit.WarningLow,
                    WarningHigh = limit.WarningHigh,
                    AlarmLow = limit.AlarmLow,
                    AlarmHigh = limit.AlarmHigh,
                })
                .ToList(),
        };
    }
}
