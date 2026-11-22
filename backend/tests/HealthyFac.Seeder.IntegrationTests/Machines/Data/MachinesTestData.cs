using HealthyFac.Core.Domain.Machines;

namespace HealthyFac.Seeder.IntegrationTests.Machines.Data;

public static class MachinesTestData
{
    public const int MachineCount = 3;
    public const int SecondSetMachineCount = 6;
    public const int MetricCountOfSet = 40;
    public const int StatusCountOfSet = 84;
    public const int PhaseCountOfSet = 7;
    public const int GoLiveDaysBefore = 10;
    public const string CoolantKey = "coolant.temperature";
    public const string CompressorName = "RSV-37 (variable speed drive)";

    public static readonly IReadOnlyList<MachineState> CompressorStates =
        [MachineState.Operational, MachineState.InMaintenance, MachineState.Operational];
}
