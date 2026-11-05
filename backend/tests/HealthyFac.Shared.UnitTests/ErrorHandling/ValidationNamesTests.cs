using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.UnitTests.ErrorHandling.Data;

namespace HealthyFac.Shared.UnitTests.ErrorHandling;

public sealed class ValidationNamesTests
{
    public ValidationNamesTests()
    {
        ValidationNames.UseJsonNames();
    }

    [Theory]
    [MemberData(nameof(ValidationNamesTestData.Keys), MemberType = typeof(ValidationNamesTestData))]
    public void UseJsonNames_WithFailedRule_KeysTheErrorByJsonName(NamedRequest request, string expectedKey)
    {
        var result = new NamedRequestValidator().Validate(request);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(expectedKey);
    }

    [Fact]
    public void UseJsonNames_WithPascalCaseMember_KeepsSplitDisplayNameInMessage()
    {
        var result = new NamedRequestValidator().Validate(new NamedRequest());

        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Contain("'Serial Number'");
    }

    [Fact]
    public void UseJsonNames_CalledTwice_ChangesNothing()
    {
        ValidationNames.UseJsonNames();
        var result = new NamedRequestValidator().Validate(new NamedRequest());

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("serialNumber");
    }
}
