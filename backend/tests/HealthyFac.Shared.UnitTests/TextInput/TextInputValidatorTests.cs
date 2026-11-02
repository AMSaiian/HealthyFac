using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.UnitTests.TextInput.Data;

namespace HealthyFac.Shared.UnitTests.TextInput;

public sealed class TextInputValidatorTests
{
    [Theory]
    [MemberData(nameof(TextInputTestData.Violations), MemberType = typeof(TextInputTestData))]
    public void Validate_WithForbiddenCharacter_ReportsFieldAndRuleWithoutValue(
        TextInputRequest request,
        string expectedPath,
        string expectedMessage
    )
    {
        var result = new TextInputValidator<TextInputRequest>().Validate(request);

        var failure = result.Errors.Should().ContainSingle().Which;
        failure.PropertyName.Should().Be(expectedPath);
        failure.ErrorMessage.Should().Be(expectedMessage);
        failure.AttemptedValue.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(TextInputTestData.ValidRequests), MemberType = typeof(TextInputTestData))]
    public void Validate_WithAllowedText_IsValid(TextInputRequest request)
    {
        var result = new TextInputValidator<TextInputRequest>().Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithReferenceCycle_EndsAndReportsOnce()
    {
        var node = new TextInputNode { Label = TextInputTestData.NullCharacterText };
        node.Next = node;

        var result = new TextInputValidator<TextInputNode>().Validate(node);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(TextInputNode.Label));
    }
}
