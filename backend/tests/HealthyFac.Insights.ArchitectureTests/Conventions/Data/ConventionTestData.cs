using System.Collections.Frozen;
using HealthyFac.Insights.ArchitectureTests.Dependencies.Data;

namespace HealthyFac.Insights.ArchitectureTests.Conventions.Data;

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
        LayerDependencyTestData.InsightsDomain,
        LayerDependencyTestData.InsightsApplication,
        LayerDependencyTestData.InsightsInfrastructure,
        LayerDependencyTestData.InsightsWeb,
        LayerDependencyTestData.SharedDomain,
        LayerDependencyTestData.SharedApplication,
        LayerDependencyTestData.SharedInfrastructure,
        LayerDependencyTestData.SharedWeb,
    ];

    public static TheoryData<string> ApplicationAssemblies =>
    [
        LayerDependencyTestData.InsightsApplication,
        LayerDependencyTestData.SharedApplication,
    ];

    public static readonly string[] UseCaseAssemblies =
    [
        LayerDependencyTestData.InsightsApplication,
        LayerDependencyTestData.SharedApplication,
    ];

    public static readonly string[] MessageAssemblies =
    [
        LayerDependencyTestData.InsightsApplication,
        LayerDependencyTestData.InsightsInfrastructure,
        LayerDependencyTestData.InsightsWeb,
        LayerDependencyTestData.CoreMessaging,
        LayerDependencyTestData.InsightsMessaging,
        LayerDependencyTestData.DocumentMessaging,
        LayerDependencyTestData.TelemetryMessaging,
    ];

    public static readonly string[] ContractAssemblies =
    [
        LayerDependencyTestData.DocumentContracts,
    ];
}
