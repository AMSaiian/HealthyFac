using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.UnitTests.Caching.Data;

namespace HealthyFac.Shared.UnitTests.Caching;

public sealed class RedisOptionsValidatorTests
{
    private readonly RedisOptionsValidator _validator = new();

    [Theory]
    [MemberData(
        nameof(RedisOptionsValidatorTestData.ValidTimeouts),
        MemberType = typeof(RedisOptionsValidatorTestData)
    )]
    public void Validate_WhenTimeoutsAreInRange_ShouldBeValid(RedisOptions options)
    {
        var result = _validator.Validate(options);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [MemberData(
        nameof(RedisOptionsValidatorTestData.InvalidTimeoutsWithExpectedError),
        MemberType = typeof(RedisOptionsValidatorTestData)
    )]
    public void Validate_WhenTimeoutIsOutOfRange_ShouldReportExpectedError(
        RedisOptions options,
        string expectedErrorMessage
    )
    {
        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == expectedErrorMessage);
    }
}
