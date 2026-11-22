namespace HealthyFac.Seeder.Cases;

/// <summary>
/// The content of <c>cases.json</c>: the open maintenance case with its signal and its work order draft.
/// </summary>
public sealed class CasesFile
{
    public required CaseSeed Case { get; init; }

    public required SignalSeed Signal { get; init; }

    public required WorkOrderDraftSeed WorkOrderDraft { get; init; }
}
