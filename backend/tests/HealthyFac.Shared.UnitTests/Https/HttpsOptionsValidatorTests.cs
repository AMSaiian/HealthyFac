using HealthyFac.Shared.UnitTests.Https.Data;
using HealthyFac.Shared.Web.Https;
using HealthyFac.Testing.Features;

namespace HealthyFac.Shared.UnitTests.Https;

[Feature(TestFeatures.Platform)]
public sealed class HttpsOptionsValidatorTests
{
    private readonly HttpsOptionsValidator _validator = new();

    [Fact]
    public void Validate_WithPreloadAtMinimumMaxAge_IsValid()
    {
        var result = _validator.Validate(HttpsOptionsValidatorTestData.PreloadAtMinimumMaxAge);

        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithPreloadBelowMinimumMaxAge_ReportsPreloadRequirementsNotMet()
    {
        var result = _validator.Validate(HttpsOptionsValidatorTestData.PreloadBelowMinimumMaxAge);

        result.Errors.Should()
            .ContainSingle()
            .Which.ErrorMessage.Should()
            .Be(HttpsValidationMessages.PreloadRequirementsNotMet);
    }
}
