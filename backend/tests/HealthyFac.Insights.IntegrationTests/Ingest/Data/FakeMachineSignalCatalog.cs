using HealthyFac.Insights.Application.Extraction.Signals;

namespace HealthyFac.Insights.IntegrationTests.Ingest.Data;

/// <summary>
/// Answers with <see cref="Signals"/>. The default is no metrics and no statuses, like a machine without definitions.
/// </summary>
public sealed class FakeMachineSignalCatalog : IMachineSignalCatalog
{
    public List<Guid> AskedMachines { get; } = [];

    public MachineSignals Signals { get; set; } = Empty();

    public void Reset()
    {
        AskedMachines.Clear();
        Signals = Empty();
    }

    public Task<MachineSignals> GetAsync(Guid machineId, CancellationToken cancellationToken)
    {
        AskedMachines.Add(machineId);

        return Task.FromResult(Signals);
    }

    private static MachineSignals Empty() => new() { Metrics = [], Statuses = [] };
}
