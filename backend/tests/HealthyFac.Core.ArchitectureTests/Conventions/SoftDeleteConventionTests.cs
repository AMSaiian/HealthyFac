using HealthyFac.Core.ArchitectureTests.Conventions.Data;
using HealthyFac.Core.ArchitectureTests.Dependencies.Data;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.ArchitectureTests.Conventions;

public sealed class SoftDeleteConventionTests
{
    [Theory]
    [MemberData(nameof(SoftDeleteTestData.EntityAssemblies), MemberType = typeof(SoftDeleteTestData))]
    public void Entities_InAssembly_ImplementSoftDeletable(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine,
            LayerDependencyTestData.Resolve(assemblyName)
                .GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IEntity).IsAssignableFrom(type))
                .Where(type => !typeof(ISoftDeletable).IsAssignableFrom(type))
                .Select(type => type.FullName!)
                .Order());

        violations.Should().BeEmpty();
    }
}
