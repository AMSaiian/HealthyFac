using System.Text;
using System.Text.Json.Nodes;
using HealthyFac.Telemetry.Domain.Intake;

namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

public static class ReceiveMachineMessageTestData
{
    public const string TemperatureKey = "spindle.temperature";
    public const string DoorKey = "door.open";
    public const string ModeKey = "spindle.mode";
    public const string ErrorStatus = "E042";
    public const string WarningStatus = "W007";
    public const string UnknownStatus = "X999";
    public const string UnknownGatewayId = "zzzzzzzzzzzzzzzz";

    public const string DuplicateMetricKeyMessage =
        """{"sampledAt":"2026-11-06T10:14:55.0000000Z","metrics":{"spindle.temperature":1,"spindle."""
        + """temperature":2},"statuses":[]}""";

    public static readonly DateTime ReceivedAt = new(
        year: 2026, month: 11, day: 6, hour: 10, minute: 15, second: 0, DateTimeKind.Utc);

    public static readonly DateTime SampledAt = ReceivedAt.AddSeconds(value: -5);

    public static string TopicFor(string gatewayId) => $"healthyfac/gateways/{gatewayId}/messages";

    public static JsonObject CreateMessage(
        DateTime? sampledAt = null,
        JsonNode? temperature = null,
        JsonNode? door = null,
        JsonNode? mode = null,
        string[]? statuses = null
    ) =>
        new()
        {
            [propertyName: "sampledAt"] = (sampledAt ?? SampledAt).ToString(format: "O"),
            [propertyName: "metrics"] = new JsonObject
            {
                [TemperatureKey] = temperature ?? 71.5,
                [DoorKey] = door ?? true,
                [ModeKey] = mode ?? "auto",
            },
            [propertyName: "statuses"] = new JsonArray(
                (statuses ?? [ErrorStatus, WarningStatus]).Select(code => (JsonNode)code).ToArray()),
        };

    public static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());

    public static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    public static TheoryData<double> OutOfRangeTemperatures => new() { -20.1, 200.1 };

    public static TheoryData<double> RangeEdgeTemperatures => new() { -20.0, 200.0 };

    public static TheoryData<string> MalformedTopics => new()
    {
        "healthyfac/gateways/BADID/messages",
        "healthyfac/gateways/abc/messages",
        "other/topic",
        "healthyfac/gateways//messages",
    };

    public static TheoryData<string> LoneSurrogateMessages => new()
    {
        """{"sampledAt":"2026-11-06T10:14:55.0000000Z","metrics":{"spindle.mode":"\ud800"},"statuses":[]}""",
        """{"sampledAt":"2026-11-06T10:14:55.0000000Z","metrics":{"bad\ud800key":1},"statuses":[]}""",
        """{"sampledAt":"2026-11-06T10:14:55.0000000Z","metrics":{},"statuses":["\ud800"]}""",
    };

    public static TheoryData<string, string> EnvelopeFailures => new()
    {
        { """{"sampledAt":"2026-11-06T10:14:55Z","statuses":[]}""", IntakeErrorCodes.MissingField },
        { """{"sampledAt":"2026-11-06T10:14:55Z","metrics":{}}""", IntakeErrorCodes.MissingField },
        { """{"metrics":{},"statuses":[]}""", IntakeErrorCodes.MissingField },
        {
            """{"sampledAt":"2026-11-06T10:14:55Z","gatewayId":"abc","metrics":{},"statuses":[]}""",
            IntakeErrorCodes.UnknownField
        },
        { """{"sampledAt":"yesterday","metrics":{},"statuses":[]}""", IntakeErrorCodes.InvalidSampledAt },
        { """{"sampledAt":20261106,"metrics":{},"statuses":[]}""", IntakeErrorCodes.InvalidSampledAt },
        { """{"sampledAt":"2026-11-06T10:14:55Z","metrics":[],"statuses":[]}""", IntakeErrorCodes.WrongType },
        { """{"sampledAt":"2026-11-06T10:14:55Z","metrics":{},"statuses":"E042"}""", IntakeErrorCodes.WrongType },
        { """{"sampledAt":"2026-11-06T10:14:55Z","metrics":{},"statuses":[42]}""", IntakeErrorCodes.WrongType },
        { """[1,2,3]""", IntakeErrorCodes.WrongType },
        { """{"sampledAt":"2026-11-06T10:16:00.001Z","metrics":{},"statuses":[]}""", IntakeErrorCodes.FutureSampledAt },
    };
}
