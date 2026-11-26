using HealthyFac.Shared.UnitTests.Proxy.Data;
using HealthyFac.Shared.Web.Proxy;
using HealthyFac.Testing.Features;

namespace HealthyFac.Shared.UnitTests.Proxy;

[Feature(TestFeatures.Gateway)]
public sealed class ProxyOptionsValidatorTests
{
    private readonly ProxyOptionsValidator _validator = new();

    [Fact]
    public void Validate_WithValidProxyAndNetwork_IsValid()
    {
        var result = _validator.Validate(ProxyOptionsValidatorTestData.ValidAddresses);

        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [MemberData(
        nameof(ProxyOptionsValidatorTestData.InvalidAddressesWithExpectedError),
        MemberType = typeof(ProxyOptionsValidatorTestData)
    )]
    public void Validate_WithInvalidAddress_ReportsExpectedError(ProxyOptions options, string expectedErrorMessage)
    {
        var result = _validator.Validate(options);

        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(expectedErrorMessage);
    }
}
