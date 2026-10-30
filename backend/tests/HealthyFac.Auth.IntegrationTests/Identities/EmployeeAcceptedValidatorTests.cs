using HealthyFac.Auth.Application.Identities.Messages;
using HealthyFac.Auth.IntegrationTests.Identities.Data;

namespace HealthyFac.Auth.IntegrationTests.Identities;

public sealed class EmployeeAcceptedValidatorTests
{
    private readonly EmployeeAcceptedValidator _validator = new();

    [Theory]
    [MemberData(
        nameof(ProvisioningTestData.ValidMessages),
        MemberType = typeof(ProvisioningTestData)
    )]
    public void Validate_WithValidMessage_IsValid(EmployeeAccepted message)
    {
        var result = _validator.Validate(message);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(ProvisioningTestData.InvalidMessagesWithExpectedError),
        MemberType = typeof(ProvisioningTestData)
    )]
    public void Validate_WithInvalidMessage_ReportsExpectedError(EmployeeAccepted message, string expectedErrorMessage)
    {
        var result = _validator.Validate(message);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == expectedErrorMessage);
    }
}
