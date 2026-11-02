using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.UnitTests.Configuration.Data;

namespace HealthyFac.Shared.UnitTests.Configuration;

public sealed class UriRuleBuilderExtensionsTests
{
    [Theory]
    [MemberData(
        nameof(UriRuleBuilderExtensionsTestData.AbsoluteHttpUris),
        MemberType = typeof(UriRuleBuilderExtensionsTestData)
    )]
    public void MustBeAbsoluteHttpUri_WithAbsoluteHttpUri_IsValid(string value)
    {
        var validator = UriRuleBuilderExtensionsTestData.CreateValidator();

        var result = validator.Validate(new UriSetting { Value = value });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(UriRuleBuilderExtensionsTestData.OtherValues),
        MemberType = typeof(UriRuleBuilderExtensionsTestData)
    )]
    public void MustBeAbsoluteHttpUri_WithOtherValue_ReportsUriMustBeAbsoluteHttp(string value)
    {
        var validator = UriRuleBuilderExtensionsTestData.CreateValidator();

        var result = validator.Validate(new UriSetting { Value = value });

        result.Errors.Should()
            .ContainSingle(error => error.ErrorMessage == ConfigurationValidationMessages.UriMustBeAbsoluteHttp);
    }
}
