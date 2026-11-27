using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Seeder.Ingest;
using HealthyFac.Seeder.Machines;
using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.People;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.Cases;

/// <summary>
/// Seeds the open maintenance case with its drafted signal from <c>cases.json</c>. The case and the signal go through
/// the domain of Insights, because the detection and the planner agent need telemetry and an AI model. The same save
/// adds the <see cref="WorkOrderDraftProposed"/> message, so the relay stores the work order draft in Core.
/// </summary>
public sealed class CasesPart : ISeedPart
{
    private readonly ServiceHosts.ServiceHosts _hosts;

    private CasesFile _file = null!;

    public CasesPart(ServiceHosts.ServiceHosts hosts)
    {
        _hosts = hosts;
    }

    public IReadOnlyList<SeedEvent> ReadEvents(SeedFolder folder)
    {
        _file = folder.Read<CasesFile>(CasesConstants.FileName);
        var item = SeedItem.Of(CasesConstants.FileName, CasesConstants.CaseKey);

        if (_file.Signal.Coverage != SignalCoverage.Drafted)
        {
            throw new SeedFileException(CasesErrors.CoverageNotSupported(item, _file.Signal.Coverage.Value));
        }

        CheckMetricKey(folder, item);

        return
        [
            new SeedEvent
            {
                At = _file.Case.OpenedAt,
                Item = item,
                Creates = [CasesConstants.CaseKey],
                Uses = [PeopleConstants.CompanyKey, $"{_file.Case.Machine}{CasesConstants.MachineSuffix}"],
            },
        ];
    }

    public async Task RunAsync(SeedEvent seedEvent, SeedContext context, CancellationToken cancellationToken)
    {
        var companyId = context.GetId(PeopleConstants.CompanyKey);
        var machineId = context.GetId($"{_file.Case.Machine}{CasesConstants.MachineSuffix}");
        var openedAt = SeedCalendar.ToUtc(_file.Case.OpenedAt, context.Anchor, context.Zone).UtcDateTime;

        using var scope =
            _hosts.Insights.CreateScope(ServiceActor.DatabaseInitialization.For(ServiceName.Insights, companyId));

        var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

        var maintenanceCase = new MaintenanceCase(companyId, machineId, openedAt);

        dbContext.MaintenanceCases.Add(maintenanceCase).Property(nameof(MaintenanceCase.Id)).CurrentValue =
            _file.Case.Id;

        var signal = new MachineSignal(
            _file.Signal.Id,
            machineId,
            companyId,
            new SignalChange
            {
                Rule = _file.Signal.Rule,
                Severity = _file.Signal.Severity,
                State = _file.Signal.State,
                MetricKey = _file.Signal.MetricKey,
                Value = _file.Signal.Value,
                Limit = _file.Signal.Limit,
                OpenedAt = openedAt,
                Sequence = CasesConstants.FirstSequence,
            });

        signal.MarkDrafted(_file.WorkOrderDraft.DraftId);
        maintenanceCase.Add(signal);

        scope.ServiceProvider.GetRequiredService<IOutbox>()
            .Add(
                new WorkOrderDraftProposed
                {
                    Source = WorkOrderProposalSource.Case,
                    DraftId = _file.WorkOrderDraft.DraftId,
                    CompanyId = companyId,
                    MachineId = machineId,
                    CaseId = _file.Case.Id,
                    RunId = _file.WorkOrderDraft.RunId,
                    Title = _file.WorkOrderDraft.Title,
                    Description = _file.WorkOrderDraft.Description,
                    Priority = _file.WorkOrderDraft.Priority,
                    Category = _file.WorkOrderDraft.Category,
                    EstimateMinutes = _file.WorkOrderDraft.EstimateMinutes,
                });

        await dbContext.SaveChangesAsync(cancellationToken);
        context.SetId(CasesConstants.CaseKey, _file.Case.Id);
    }

    private void CheckMetricKey(SeedFolder folder, string item)
    {
        var machinesFile = folder.Read<MachinesFile>(MachinesConstants.FileName);
        var machine = machinesFile.Machines.FirstOrDefault(candidate => candidate.Key == _file.Case.Machine);

        if (machine is null)
        {
            return;
        }

        var draft = folder.Read<IngestSetFile>(MachinesConstants.SetFileName)
            .Entries
            .SelectMany(entry => entry.DraftsProposed.Machines)
            .FirstOrDefault(candidate => candidate.DraftId == machine.DraftId);

        if (draft is not null && draft.Metrics.All(metric => metric.Key != _file.Signal.MetricKey))
        {
            throw new SeedFileException(CasesErrors.MetricUnknown(item, _file.Signal.MetricKey, machine.Key));
        }
    }
}
