using System.Text.Json;
using HealthyFac.Shared.Infrastructure.TextInput;
using HealthyFac.Shared.UnitTests.TextInput.Data;

namespace HealthyFac.Shared.UnitTests.TextInput;

public sealed class TextInputJsonTests
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    [Theory]
    [MemberData(nameof(TextInputTestData.NormalizedJson), MemberType = typeof(TextInputTestData))]
    public void Deserialize_WithTextInput_NormalizesByForm(string json, TextInputRequest expected)
    {
        var request = JsonSerializer.Deserialize<TextInputRequest>(json, Options);

        request.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Serialize_WithOuterSpaces_WritesTextUnchanged()
    {
        var json = JsonSerializer.Serialize(new TextInputRequest { Name = " padded " }, Options);

        json.Should().Contain(expected: "\" padded \"");
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions.Web);
        TextInputJson.Configure(options);

        return options;
    }
}
