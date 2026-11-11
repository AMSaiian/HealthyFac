using System.Collections.Frozen;
using HealthyFac.Document.ArchitectureTests.Dependencies.Data;

namespace HealthyFac.Document.ArchitectureTests.Conventions.Data;

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
        LayerDependencyTestData.DocumentDomain,
        LayerDependencyTestData.DocumentApplication,
        LayerDependencyTestData.DocumentInfrastructure,
        LayerDependencyTestData.DocumentWeb,
        LayerDependencyTestData.SharedDomain,
        LayerDependencyTestData.SharedApplication,
        LayerDependencyTestData.SharedInfrastructure,
        LayerDependencyTestData.SharedWeb,
    ];

    public static TheoryData<string> ApplicationAssemblies =>
    [
        LayerDependencyTestData.DocumentApplication,
        LayerDependencyTestData.SharedApplication,
    ];

    public static readonly string[] UseCaseAssemblies =
    [
        LayerDependencyTestData.DocumentApplication,
        LayerDependencyTestData.SharedApplication,
    ];

    public static readonly string[] MessageAssemblies =
    [
        LayerDependencyTestData.DocumentApplication,
        LayerDependencyTestData.DocumentInfrastructure,
        LayerDependencyTestData.DocumentWeb,
        LayerDependencyTestData.CoreMessaging,
        LayerDependencyTestData.InsightsMessaging,
        LayerDependencyTestData.DocumentMessaging,
        LayerDependencyTestData.TelemetryMessaging,
    ];

    public static readonly string[] ContractAssemblies =
    [
        LayerDependencyTestData.DocumentContracts,
    ];

    public static readonly string[] GrpcServiceAssemblies =
    [
        LayerDependencyTestData.DocumentWeb,
    ];
}
