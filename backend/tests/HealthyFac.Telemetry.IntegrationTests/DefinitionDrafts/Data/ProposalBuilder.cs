using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

/// <summary>
/// Builds the proposal messages of the definition draft tests.
/// </summary>
public static class ProposalBuilder
{
    public static DraftsProposed Message(Guid companyId, params ProposedMachine[] machines) =>
        new()
        {
            AttemptId = DefinitionDraftTestData.AttemptId,
            FileId = DefinitionDraftTestData.FileId,
            CompanyId = companyId,
            IsMachineFile = false,
            QueuedAt = new DateTime(year: 2026, month: 11, day: 17, hour: 8, minute: 0, second: 0, DateTimeKind.Utc),
            Machines = machines,
        };

    public static ProposedMachine Machine(Guid? targetMachineId = null, int metrics = 1, int statuses = 1) =>
        new()
        {
            DraftId = Guid.CreateVersion7(),
            TargetMachineId = targetMachineId,
            Metrics =
            [
                .. Enumerable.Range(start: 0, metrics)
                    .Select(index => new ProposedMetric
                    {
                        DraftId = Guid.CreateVersion7(),
                        SourceLocation = $"p.{index}",
                        Key = $"motor.temperature_{index}",
                    }),
            ],
            Statuses =
            [
                .. Enumerable.Range(start: 0, statuses)
                    .Select(index => new ProposedStatus
                    {
                        DraftId = Guid.CreateVersion7(),
                        SourceLocation = $"p.{index}",
                        Code = $"E10{index}",
                    }),
            ],
        };
}
