using HealthyFac.Core.ArchitectureTests.Dependencies.Data;

namespace HealthyFac.Core.ArchitectureTests.Conventions.Data;

public static class SoftDeleteTestData
{
    public static TheoryData<string> EntityAssemblies =>
    [
        LayerDependencyTestData.CoreDomain,
    ];
}
