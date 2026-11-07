using System.Collections.Frozen;
using HealthyFac.Telemetry.ArchitectureTests.Dependencies.Data;

namespace HealthyFac.Telemetry.ArchitectureTests.Conventions.Data;

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

    public static TheoryData<string> AllAssemblies =>
    [
        LayerDependencyTestData.TelemetryDomain,
        LayerDependencyTestData.TelemetryApplication,
        LayerDependencyTestData.TelemetryInfrastructure,
        LayerDependencyTestData.TelemetryWeb,
        LayerDependencyTestData.SharedDomain,
        LayerDependencyTestData.SharedApplication,
        LayerDependencyTestData.SharedInfrastructure,
        LayerDependencyTestData.SharedWeb,
    ];

    public static TheoryData<string> ApplicationAssemblies =>
    [
        LayerDependencyTestData.TelemetryApplication,
        LayerDependencyTestData.SharedApplication,
    ];

    public static readonly string[] UseCaseAssemblies =
    [
        LayerDependencyTestData.TelemetryApplication,
        LayerDependencyTestData.SharedApplication,
    ];
}
