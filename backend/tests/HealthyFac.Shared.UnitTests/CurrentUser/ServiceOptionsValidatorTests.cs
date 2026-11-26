using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using HealthyFac.Shared.UnitTests.CurrentUser.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Shared.UnitTests.CurrentUser;

[Feature(TestFeatures.Identity)]
public sealed class ServiceOptionsValidatorTests
{
    private readonly ServiceOptionsValidator _validator = new();

    [Theory]
    [MemberData(nameof(ServiceOptionsTestData.ValidOptions), MemberType = typeof(ServiceOptionsTestData))]
    public void Validate_WithKnownServiceName_IsValid(ServiceOptions options)
    {
        var result = _validator.Validate(options);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(ServiceOptionsTestData.InvalidOptions), MemberType = typeof(ServiceOptionsTestData))]
    public void Validate_WithUnknownServiceName_ReportsExpectedError(ServiceOptions options)
    {
        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == ServiceValidationMessages.NameInvalid);
    }
}
