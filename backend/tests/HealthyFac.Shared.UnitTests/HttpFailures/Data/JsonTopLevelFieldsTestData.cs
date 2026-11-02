using System.Text.Json;

namespace HealthyFac.Shared.UnitTests.HttpFailures.Data;

public static class JsonTopLevelFieldsTestData
{
    public const string FieldName = "code";
    public const string UnescapedText = "a\tb";

    public static readonly string NullBody = """{"code":null,"message":"text"}""";

    public static readonly string EscapedStringBody = """{"code":"a\tb"}""";

    public static TheoryData<string, string> NumberBodies =>
        new()
        {
            { """{"code":4012}""", "4012" },
            { """{"code":1.5e3}""", "1.5e3" },
        };

    public static TheoryData<string, JsonValueKind, string> BooleanBodies =>
        new()
        {
            { """{"code":true}""", JsonValueKind.True, "true" },
            { """{"code":false}""", JsonValueKind.False, "false" },
        };
}
