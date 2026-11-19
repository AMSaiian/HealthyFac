namespace HealthyFac.Telemetry.ApiTests.MetricDefinitions.Data;

public static class MetricLimitsApiTestData
{
    public const string NumericPrefix =
        "{\"expectedRevision\":0,\"created\":[{\"key\":\"spindle.temperature\",\"name\":\"Spindle temperature\","
        + "\"valueKind\":\"numeric\",\"unit\":{\"quantityKind\":\"Temperature\",\"name\":\"DegreeCelsius\"},";

    public const string LimitsBody = NumericPrefix
        + "\"warningLow\":10,\"warningHigh\":80,\"alarmLow\":5,\"alarmHigh\":90,\"behavior\":\"counter\"}]}";

    public const string LimitsWithoutBehaviorBody = NumericPrefix + "\"alarmHigh\":90}]}";

    public const string UpdateBodyTemplate =
        "{\"expectedRevision\":1,\"updated\":[{\"id\":\"{id}\",\"name\":\"Spindle temperature\","
        + "\"warningHigh\":70,\"alarmHigh\":95,\"behavior\":\"level\"}]}";

    public const string UpdateWithoutLimitsBodyTemplate =
        "{\"expectedRevision\":1,\"updated\":[{\"id\":\"{id}\",\"name\":\"Spindle temperature\"}]}";

    public const string UpdateWithBadOrderBodyTemplate =
        "{\"expectedRevision\":1,\"updated\":[{\"id\":\"{id}\",\"name\":\"Spindle temperature\","
        + "\"warningLow\":1,\"alarmLow\":5}]}";

    public static TheoryData<string, string, string> InvalidCreateBodies => new()
    {
        {
            "warning low below alarm low",
            NumericPrefix + "\"warningLow\":1,\"alarmLow\":5}]}",
            "created[0].warningLow"
        },
        {
            "warning high above alarm high",
            NumericPrefix + "\"warningHigh\":95,\"alarmHigh\":90}]}",
            "created[0].warningHigh"
        },
        {
            "limit on a boolean metric",
            "{\"expectedRevision\":0,\"created\":[{\"key\":\"door.open\",\"name\":\"Door\",\"valueKind\":\"boolean\","
            + "\"warningHigh\":1}]}",
            "created[0].warningHigh"
        },
        {
            "limit on a state set metric",
            "{\"expectedRevision\":0,\"created\":[{\"key\":\"mode\",\"name\":\"Mode\",\"valueKind\":\"stateSet\","
            + "\"states\":[\"idle\",\"run\"],\"alarmLow\":1}]}",
            "created[0].alarmLow"
        },
    };
}
