using HealthyFac.Core.Application.Employees.Messages;
using HealthyFac.Core.UnitTests.Employees.Data;

namespace HealthyFac.Core.UnitTests.Employees;

public sealed class IdentityProvisioningValidatorTests
{
    private readonly IdentityProvisionedValidator _provisionedValidator = new();
    private readonly IdentityProvisioningFailedValidator _failedValidator = new();

    [Fact]
    public async Task ValidateAsync_WithValidProvisionedMessage_IsValid()
    {
        var result = await _provisionedValidator.ValidateAsync(IdentityProvisioningValidatorTestData.ValidProvisioned);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(IdentityProvisioningValidatorTestData.InvalidProvisionedWithProperty),
        MemberType = typeof(IdentityProvisioningValidatorTestData)
    )]
    public async Task ValidateAsync_WithInvalidProvisionedMessage_FailsOnMatchingProperty(
        IdentityProvisioned message,
        string expectedProperty
    )
    {
        var result = await _provisionedValidator.ValidateAsync(message);

        result.Errors.Should().ContainSingle(error => error.PropertyName == expectedProperty);
    }

    [Fact]
    public async Task ValidateAsync_WithValidFailedMessage_IsValid()
    {
        var result = await _failedValidator.ValidateAsync(IdentityProvisioningValidatorTestData.ValidFailed);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(IdentityProvisioningValidatorTestData.InvalidFailedWithProperty),
        MemberType = typeof(IdentityProvisioningValidatorTestData)
    )]
    public async Task ValidateAsync_WithInvalidFailedMessage_FailsOnMatchingProperty(
        IdentityProvisioningFailed message,
        string expectedProperty
    )
    {
        var result = await _failedValidator.ValidateAsync(message);

        result.Errors.Should().ContainSingle(error => error.PropertyName == expectedProperty);
    }
}
