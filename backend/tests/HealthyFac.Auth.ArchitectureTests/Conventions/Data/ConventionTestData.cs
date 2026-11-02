using System.Collections.Frozen;
using HealthyFac.Auth.ArchitectureTests.Dependencies.Data;

namespace HealthyFac.Auth.ArchitectureTests.Conventions.Data;

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
        LayerDependencyTestData.AuthApplication,
        LayerDependencyTestData.AuthInfrastructure,
        LayerDependencyTestData.AuthWeb,
    ];

    public static TheoryData<string> ApplicationAssemblies =>
    [
        LayerDependencyTestData.AuthApplication,
    ];
}
