using HealthyFac.Auth.Application.Identities.RevokeIdentity;
using HealthyFac.Auth.IntegrationTests.Identities.Data;

namespace HealthyFac.Auth.IntegrationTests.Identities;

public sealed class EmployeeDeactivatedValidatorTests
{
    private readonly EmployeeDeactivatedValidator _validator = new();

    [Theory]
    [MemberData(
        nameof(ProvisioningTestData.ValidDeactivatedMessages),
        MemberType = typeof(ProvisioningTestData)
    )]
    public void Validate_WithValidMessage_IsValid(EmployeeDeactivated message)
    {
        var result = _validator.Validate(message);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(ProvisioningTestData.InvalidDeactivatedMessagesWithExpectedError),
        MemberType = typeof(ProvisioningTestData)
    )]
    public void Validate_WithInvalidMessage_ReportsExpectedError(
        EmployeeDeactivated message,
        string expectedErrorMessage
    )
    {
        var result = _validator.Validate(message);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == expectedErrorMessage);
    }
}
