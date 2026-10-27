using HealthyFac.Core.Application.Employees.InviteLinks;
using HealthyFac.Core.Infrastructure.Employees.InviteLinks;
using HealthyFac.Core.UnitTests.Employees.InviteLinks.Data;

namespace HealthyFac.Core.UnitTests.Employees.InviteLinks;

public sealed class InviteLinkOptionsValidatorTests
{
    private readonly InviteLinkOptionsValidator _validator = new();

    [Theory]
    [MemberData(
        nameof(InviteLinkOptionsValidatorTestData.ValidBaseUrls),
        MemberType = typeof(InviteLinkOptionsValidatorTestData)
    )]
    public void Validate_WithAbsoluteHttpUrl_IsValid(string spaBaseUrl)
    {
        var result = _validator.Validate(new InviteLinkOptions { SpaBaseUrl = spaBaseUrl });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(InviteLinkOptionsValidatorTestData.InvalidBaseUrls),
        MemberType = typeof(InviteLinkOptionsValidatorTestData)
    )]
    public void Validate_WithOtherValue_ReportsInvalidBaseUrl(string spaBaseUrl)
    {
        var result = _validator.Validate(new InviteLinkOptions { SpaBaseUrl = spaBaseUrl });

        result.IsValid.Should().BeFalse();
        result.Errors.Should()
            .ContainSingle(error => error.ErrorMessage == InviteLinkValidationMessages.SpaBaseUrlInvalid);
    }
}
