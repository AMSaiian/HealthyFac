using HealthyFac.Shared.UnitTests.CurrentUser.Data;
using HealthyFac.Shared.Web.CurrentUser;

namespace HealthyFac.Shared.UnitTests.CurrentUser;

public sealed class ServiceActorOptionsValidatorTests
{
    private readonly ServiceActorOptionsValidator _validator = new();

    [Theory]
    [MemberData(nameof(ServiceActorOptionsTestData.ValidOptions), MemberType = typeof(ServiceActorOptionsTestData))]
    public void Validate_WithDistinctClientsOrNone_IsValid(ServiceActorOptions options)
    {
        var result = _validator.Validate(options);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(ServiceActorOptionsTestData.InvalidOptionsWithExpectedError),
        MemberType = typeof(ServiceActorOptionsTestData)
    )]
    public void Validate_WithDuplicateClient_ReportsExpectedError(ServiceActorOptions options, string error)
    {
        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(failure => failure.ErrorMessage == error);
    }
}
