using HealthyFac.Insights.Application.Extraction;

namespace HealthyFac.Insights.IntegrationTests.Ingest.Data;

/// <summary>
/// Answers with no metric keys and no status codes, like a machine without definitions.
/// </summary>
public sealed class FakeMachineSignalCatalog : IMachineSignalCatalog
{
    public List<Guid> AskedMachines { get; } = [];

    public void Reset() => AskedMachines.Clear();

    public Task<MachineSignals> GetAsync(Guid machineId, CancellationToken cancellationToken)
    {
        AskedMachines.Add(machineId);

        return Task.FromResult(new MachineSignals { MetricKeys = [], StatusCodes = [] });
    }
}
