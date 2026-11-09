using HealthyFac.Document.ArchitectureTests.Dependencies.Data;

namespace HealthyFac.Document.ArchitectureTests.Conventions.Data;

public static class SoftDeleteTestData
{
    /// <summary>
    /// The append-only entities that are not soft deletable on purpose. A new exempt entity needs a deliberate entry
    /// here.
    /// </summary>
    public static readonly IReadOnlyList<string> ExemptEntityNames =
        [];

    public static TheoryData<string> EntityAssemblies =>
    [
        LayerDependencyTestData.DocumentDomain,
    ];
}
