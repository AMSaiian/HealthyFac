using System.Collections.Frozen;
using System.Reflection;

namespace HealthyFac.Seeder.ArchitectureTests.Conventions.Data;

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

    public static readonly Assembly Seeder = SeederAssembly.Instance;

    public static readonly Assembly[] SeederAssemblies = [Seeder];
}
