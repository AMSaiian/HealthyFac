using System.Collections.Frozen;
using System.Reflection;

namespace HealthyFac.MachineSimulator.ArchitectureTests.Conventions.Data;

public static class ConventionTestData
{
    public static readonly FrozenSet<string> TechnicalRootFolderNames =
        FrozenSet.Create(
            "Services",
            "Models",
            "Handlers",
            "Helpers",
            "Utils",
            "Utilities",
            "Dtos",
            "Managers"
        );

    public static readonly Assembly Simulator = MachineSimulatorAssembly.Instance;

    public static readonly Assembly[] SimulatorAssemblies = [Simulator];
}
