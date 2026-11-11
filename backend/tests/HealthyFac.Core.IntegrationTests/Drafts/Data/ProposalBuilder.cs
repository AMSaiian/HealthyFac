using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Core.IntegrationTests.Drafts.Data;

/// <summary>
/// Builds the proposal messages of the draft tests.
/// </summary>
public static class ProposalBuilder
{
    public const string MetricKey = "motor.temperature";
    public const string StatusCode = "E104";

    public static DraftsProposed Message(Guid companyId, bool isMachineFile, params ProposedMachine[] machines) =>
        new()
        {
            AttemptId = DraftTestData.AttemptId,
            FileId = DraftTestData.FileId,
            CompanyId = companyId,
            IsMachineFile = isMachineFile,
            QueuedAt = DraftTestData.QueuedAt,
            Machines = machines,
        };

    public static ProposedMachine Machine(
        Guid? targetMachineId = null,
        string? name = null,
        string? serialNumber = null,
        int metrics = 1,
        int statuses = 1
    ) =>
        new()
        {
            DraftId = Guid.CreateVersion7(),
            TargetMachineId = targetMachineId,
            Name = name,
            SerialNumber = serialNumber,
            Metrics =
            [
                .. Enumerable.Range(start: 0, metrics)
                    .Select(index => new ProposedMetric
                    {
                        DraftId = Guid.CreateVersion7(),
                        SourceLocation = $"p.{index}",
                        Key = $"{MetricKey}_{index}",
                    }),
            ],
            Statuses =
            [
                .. Enumerable.Range(start: 0, statuses)
                    .Select(index => new ProposedStatus
                    {
                        DraftId = Guid.CreateVersion7(),
                        SourceLocation = $"p.{index}",
                        Code = $"{StatusCode}{index}",
                    }),
            ],
        };
}
