using HealthyFac.Document.ArchitectureTests.Dependencies.Data;
using NetArchTest.Rules;

namespace HealthyFac.Document.ArchitectureTests.Dependencies;

public sealed class LayerDependencyTests
{
    [Theory]
    [MemberData(
        nameof(LayerDependencyTestData.ForbiddenDependencies),
        MemberType = typeof(LayerDependencyTestData)
    )]
    public void Layer_WithForbiddenNamespace_HasNoTypeThatDependsOnIt(string layer, string forbiddenNamespace)
    {
        var result = Types.InAssembly(LayerDependencyTestData.Resolve(layer))
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenNamespace)
            .GetResult();

        result.FailingTypes.Select(type => type.FullName).Should().BeEmpty();
    }
}
