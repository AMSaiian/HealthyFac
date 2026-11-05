using System.Text.Json;
using HealthyFac.Shared.Infrastructure.HttpFailures.ReasonReaders;
using HealthyFac.Shared.Infrastructure.HttpFailures;
using HealthyFac.Shared.UnitTests.HttpFailures.Data;

namespace HealthyFac.Shared.UnitTests.HttpFailures;

public sealed class JsonTopLevelFieldsTests
{
    [Theory]
    [MemberData(nameof(JsonTopLevelFieldsTestData.NumberBodies), MemberType = typeof(JsonTopLevelFieldsTestData))]
    public void Read_WithNumberField_KeepsRawTextAndKind(string json, string rawText)
    {
        var fields = HttpFailureTestData.Parse(json);

        fields.Should()
            .ContainKey(JsonTopLevelFieldsTestData.FieldName)
            .WhoseValue.Should()
            .Be(new JsonTopLevelField { Kind = JsonValueKind.Number, Text = rawText });
    }

    [Theory]
    [MemberData(nameof(JsonTopLevelFieldsTestData.BooleanBodies), MemberType = typeof(JsonTopLevelFieldsTestData))]
    public void Read_WithBooleanField_KeepsTextAndKind(string json, JsonValueKind kind, string text)
    {
        var fields = HttpFailureTestData.Parse(json);

        fields.Should()
            .ContainKey(JsonTopLevelFieldsTestData.FieldName)
            .WhoseValue.Should()
            .Be(new JsonTopLevelField { Kind = kind, Text = text });
    }

    [Fact]
    public void Read_WithNullField_LeavesItOut()
    {
        var fields = HttpFailureTestData.Parse(JsonTopLevelFieldsTestData.NullBody);

        fields.Should().NotContainKey(JsonTopLevelFieldsTestData.FieldName);
    }

    [Fact]
    public void Read_WithStringField_KeepsUnescapedText()
    {
        var fields = HttpFailureTestData.Parse(JsonTopLevelFieldsTestData.EscapedStringBody);

        fields.Should()
            .ContainKey(JsonTopLevelFieldsTestData.FieldName)
            .WhoseValue.Should()
            .Be(new JsonTopLevelField { Kind = JsonValueKind.String, Text = JsonTopLevelFieldsTestData.UnescapedText });
    }
}
