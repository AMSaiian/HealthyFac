using System.Collections.Frozen;
using HealthyFac.Core.ArchitectureTests.Dependencies.Data;

namespace HealthyFac.Core.ArchitectureTests.Conventions.Data;

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
        LayerDependencyTestData.CoreDomain,
        LayerDependencyTestData.CoreApplication,
        LayerDependencyTestData.CoreInfrastructure,
        LayerDependencyTestData.CoreWeb,
        LayerDependencyTestData.SharedDomain,
        LayerDependencyTestData.SharedApplication,
        LayerDependencyTestData.SharedInfrastructure,
        LayerDependencyTestData.SharedWeb,
    ];

    public static TheoryData<string> ApplicationAssemblies =>
    [
        LayerDependencyTestData.CoreApplication,
        LayerDependencyTestData.SharedApplication,
    ];

    public static readonly string[] UseCaseAssemblies =
    [
        LayerDependencyTestData.CoreApplication,
        LayerDependencyTestData.CoreMessaging,
        LayerDependencyTestData.SharedApplication,
    ];

    public static readonly string[] MessageAssemblies =
    [
        LayerDependencyTestData.CoreApplication,
        LayerDependencyTestData.CoreInfrastructure,
        LayerDependencyTestData.CoreWeb,
        LayerDependencyTestData.CoreMessaging,
        LayerDependencyTestData.InsightsMessaging,
        LayerDependencyTestData.DocumentMessaging,
        LayerDependencyTestData.TelemetryMessaging,
        LayerDependencyTestData.AuthMessaging,
    ];
}
