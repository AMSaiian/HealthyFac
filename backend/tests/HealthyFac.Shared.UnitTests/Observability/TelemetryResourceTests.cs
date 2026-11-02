using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.UnitTests.Observability.Data;
using Attributes = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Resource.Attributes;

namespace HealthyFac.Shared.UnitTests.Observability;

public sealed class TelemetryResourceTests
{
    [Fact]
    public void TelemetryResource_ForService_HasFourAttributes()
    {
        var attributes = TelemetryResourceHarness.BuildTracerResource(TelemetryResourceTestData.Settings);

        attributes.Should().Contain(Attributes.ServiceNamespace, ObservabilityConstants.Resource.Namespace);
        attributes.Should().Contain(Attributes.ServiceName, TelemetryResourceTestData.ServiceName);
        attributes.Should().Contain(Attributes.ServiceVersion, TelemetryResourceTestData.ExpectedVersion);
        attributes.Should()
            .Contain(Attributes.DeploymentEnvironment, TelemetryResourceTestData.ExpectedEnvironmentName);
        attributes.Should().ContainKey(Attributes.ServiceInstanceId);
    }

    [Fact]
    public void TelemetryResource_WithOtelEnvironmentVariables_UsesTheirValues()
    {
        var attributes = TelemetryResourceHarness.BuildTracerResource(TelemetryResourceTestData.OverrideSettings);

        attributes.Should().Contain(Attributes.ServiceName, TelemetryResourceTestData.OverriddenServiceName);
        attributes.Should()
            .Contain(
                Attributes.DeploymentEnvironment,
                TelemetryResourceTestData.OverriddenEnvironmentName);
        attributes.Should().Contain(Attributes.ServiceNamespace, ObservabilityConstants.Resource.Namespace);
    }
}
