using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.UnitTests.TextInput.Data;

namespace HealthyFac.Shared.UnitTests.TextInput;

public sealed class TextInputRuleTests
{
    public TextInputRuleTests()
    {
        ValidationNames.UseJsonNames();
    }

    [Theory]
    [MemberData(nameof(TextInputTestData.Violations), MemberType = typeof(TextInputTestData))]
    public void Validate_WithForbiddenCharacter_ReportsFieldAndRuleWithoutValue(
        TextInputRequest request,
        string expectedPath,
        string expectedMessage
    )
    {
        var result = new TextInputRequestValidator().Validate(request);

        var failure = result.Errors.Should().ContainSingle().Which;
        failure.PropertyName.Should().Be(expectedPath);
        failure.ErrorMessage.Should().Be(expectedMessage);
        failure.AttemptedValue.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(TextInputTestData.ValidRequests), MemberType = typeof(TextInputTestData))]
    public void Validate_WithAllowedText_IsValid(TextInputRequest request)
    {
        var result = new TextInputRequestValidator().Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
