using System.Reflection;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.ArchitectureTests.Conventions.Data;

namespace HealthyFac.Telemetry.ArchitectureTests.Conventions;

public sealed class SoftDeleteConventionTests
{
    [Theory]
    [MemberData(nameof(SoftDeleteTestData.EntityAssemblies), MemberType = typeof(SoftDeleteTestData))]
    public void Entities_InAssembly_ImplementSoftDeletable(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine,
            Assembly.Load(assemblyName)
                .GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IEntity).IsAssignableFrom(type))
                .Where(type => !typeof(ISoftDeletable).IsAssignableFrom(type))
                .Where(type => !SoftDeleteTestData.ExemptEntityNames.Contains(type.Name))
                .Select(type => type.FullName!)
                .Order());

        violations.Should().BeEmpty();
    }
}
