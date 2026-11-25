using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

/// <summary>
/// Builds the proposal messages of the definition draft tests.
/// </summary>
public static class ProposalBuilder
{
    public static DraftsProposed Message(Guid companyId, params ProposedEntry[] entries) =>
        Ingest(companyId) with
        {
            Machines = [.. entries.Select(entry => new ProposedMachine { DraftId = entry.DraftId })],
            Metrics =
            [
                .. entries.SelectMany(entry => entry.Metrics.Select(metric => metric with
                {
                    MachineDraftId = entry.DraftId,
                })),
            ],
            Statuses =
            [
                .. entries.SelectMany(entry => entry.Statuses.Select(status => status with
                {
                    MachineDraftId = entry.DraftId,
                })),
            ],
            Phases =
            [
                .. entries.SelectMany(entry => entry.Phases.Select(phase => phase with
                {
                    MachineDraftId = entry.DraftId,
                })),
            ],
        };

    public static DraftsProposed OnMachineMessage(Guid companyId, Guid machineId, int metrics = 1, int statuses = 1) =>
        Ingest(companyId) with
        {
            IsMachineFile = true,
            Metrics = [.. Metrics(metrics).Select(metric => metric with { MachineId = machineId })],
            Statuses = [.. Statuses(statuses).Select(status => status with { MachineId = machineId })],
        };

    public static DraftsProposed ChatMessage(Guid companyId, Guid machineId, int metrics = 1, int statuses = 1) =>
        new()
        {
            Source = ProposalSource.Chat,
            ChatId = DefinitionDraftTestData.ChatId,
            ChatMessageId = DefinitionDraftTestData.ChatMessageId,
            CompanyId = companyId,
            Metrics = [.. Metrics(metrics).Select(metric => metric with { MachineId = machineId })],
            Statuses = [.. Statuses(statuses).Select(status => status with { MachineId = machineId })],
        };

    public static ProposedEntry Machine(int metrics = 1, int statuses = 1) =>
        new() { Metrics = Metrics(metrics), Statuses = Statuses(statuses) };

    private static DraftsProposed Ingest(Guid companyId) =>
        new()
        {
            Source = ProposalSource.Ingest,
            AttemptId = DefinitionDraftTestData.AttemptId,
            FileId = DefinitionDraftTestData.FileId,
            CompanyId = companyId,
            IsMachineFile = false,
            QueuedAt = new DateTime(year: 2026, month: 11, day: 17, hour: 8, minute: 0, second: 0, DateTimeKind.Utc),
        };

    private static List<ProposedMetric> Metrics(int count) =>
    [
        .. Enumerable.Range(start: 0, count)
            .Select(index => new ProposedMetric
            {
                DraftId = Guid.CreateVersion7(),
                SourceLocation = $"p.{index}",
                Key = $"motor.temperature_{index}",
            }),
    ];

    private static List<ProposedStatus> Statuses(int count) =>
    [
        .. Enumerable.Range(start: 0, count)
            .Select(index => new ProposedStatus
            {
                DraftId = Guid.CreateVersion7(),
                SourceLocation = $"p.{index}",
                Code = $"E10{index}",
            }),
    ];
}
