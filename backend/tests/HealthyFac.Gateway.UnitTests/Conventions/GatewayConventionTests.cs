using HealthyFac.Gateway.Web;
using HealthyFac.Testing.Conventions;

namespace HealthyFac.Gateway.UnitTests.Conventions;

/// <summary>
/// Runs the reflection checks of <see cref="ConventionChecks"/> over the Gateway assembly. The Gateway has no
/// architecture test project, so these checks keep the shared rules for it.
/// </summary>
public sealed class GatewayConventionTests
{
    [Fact]
    public void Records_InTheGatewayAssembly_HaveNoPrimaryConstructor()
    {
        var violations = ConventionChecks.FindPositionalRecords(GatewayWebAssembly.Instance);

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Classes_InTheGatewayAssembly_UseNoCapturedPrimaryConstructorParameter()
    {
        var violations = ConventionChecks.FindCapturedPrimaryConstructorParameters(GatewayWebAssembly.Instance);

        violations.Should().BeEmpty();
    }
}
