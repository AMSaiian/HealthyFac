using HealthyFac.Core.Infrastructure.Employees.InviteTokens;
using HealthyFac.Core.UnitTests.Employees.InviteTokens.Data;

namespace HealthyFac.Core.UnitTests.Employees.InviteTokens;

public sealed class InviteTokenOptionsValidatorTests
{
    private readonly InviteTokenOptionsValidator _validator = new();

    [Fact]
    public void Validate_WithTwoKeysAndOneDefault_IsValid() =>
        _validator.Validate(InviteTokenTestData.BuildTwoKeyOptions(InviteTokenTestData.CurrentKeyId))
            .IsValid.Should()
            .BeTrue();

    [Theory]
    [MemberData(
        nameof(InviteTokenTestData.InvalidOptions),
        MemberType = typeof(InviteTokenTestData)
    )]
    public void Validate_WithInvalidOptions_IsNotValid(InviteTokenOptions options) =>
        _validator.Validate(options).IsValid.Should().BeFalse();
}
