using HealthyFac.Shared.UnitTests.HealthChecks.Data;
using HealthyFac.Shared.Web.HealthChecks;

namespace HealthyFac.Shared.UnitTests.HealthChecks;

public sealed class HealthEndpointOptionsValidatorTests
{
    private readonly HealthEndpointOptionsValidator _validator = new();

    [Theory]
    [MemberData(
        nameof(HealthEndpointOptionsValidatorTestData.PortsOutOfRange),
        MemberType = typeof(HealthEndpointOptionsValidatorTestData)
    )]
    public void Validate_WithPortOutOfRange_Fails(int port)
    {
        var result = _validator.Validate(new HealthEndpointOptions { Port = port });

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [MemberData(
        nameof(HealthEndpointOptionsValidatorTestData.ValidPorts),
        MemberType = typeof(HealthEndpointOptionsValidatorTestData)
    )]
    public void Validate_WithValidPort_Passes(int port)
    {
        var result = _validator.Validate(new HealthEndpointOptions { Port = port });

        result.IsValid.Should().BeTrue();
    }
}
