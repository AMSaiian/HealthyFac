using HealthyFac.Core.Application.Employees.Messages;
using HealthyFac.Core.UnitTests.Employees.Data;

namespace HealthyFac.Core.UnitTests.Employees;

public sealed class EmployeeInvitedValidatorTests
{
    private readonly EmployeeInvitedValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidMessage_IsValid()
    {
        var result = await _validator.ValidateAsync(EmployeeInvitedValidatorTestData.ValidMessage);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(EmployeeInvitedValidatorTestData.InvalidMessagesWithProperty),
        MemberType = typeof(EmployeeInvitedValidatorTestData)
    )]
    public async Task ValidateAsync_WithInvalidMessage_FailsOnMatchingProperty(
        EmployeeInvited message,
        string expectedProperty
    )
    {
        var result = await _validator.ValidateAsync(message);

        result.Errors.Should().ContainSingle(error => error.PropertyName == expectedProperty);
    }
}
