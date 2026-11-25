using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Core.IntegrationTests.Drafts.Data;

/// <summary>
/// Builds the proposal messages of the draft tests.
/// </summary>
public static class ProposalBuilder
{
    public static DraftsProposed Message(Guid companyId, bool isMachineFile, params ProposedMachine[] machines) =>
        new()
        {
            Source = ProposalSource.Ingest,
            AttemptId = DraftTestData.AttemptId,
            FileId = DraftTestData.FileId,
            CompanyId = companyId,
            IsMachineFile = isMachineFile,
            QueuedAt = DraftTestData.QueuedAt,
            Machines = machines,
        };

    public static DraftsProposed ChatMessage(Guid companyId, params ProposedMachine[] machines) =>
        new()
        {
            Source = ProposalSource.Chat,
            ChatId = DraftTestData.ChatId,
            ChatMessageId = DraftTestData.ChatMessageId,
            CompanyId = companyId,
            Machines = machines,
        };

    public static ProposedMachine Machine(
        Guid? targetMachineId = null,
        string? name = null,
        string? serialNumber = null
    ) =>
        new()
        {
            DraftId = Guid.CreateVersion7(),
            TargetMachineId = targetMachineId,
            Name = name,
            SerialNumber = serialNumber,
        };

    public static ProposedMetric Metric(Guid? machineId = null, Guid? machineDraftId = null) =>
        new()
        {
            DraftId = Guid.CreateVersion7(),
            MachineId = machineId,
            MachineDraftId = machineDraftId,
            Key = "motor.temperature",
        };
}
