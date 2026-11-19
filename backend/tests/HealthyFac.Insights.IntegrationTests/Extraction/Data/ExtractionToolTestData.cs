using HealthyFac.Insights.Application.Extraction.Signals;

namespace HealthyFac.Insights.IntegrationTests.Extraction.Data;

public static class ExtractionToolTestData
{
    public const string MachineCall = """{"name":"CNC Mill VMC-500","description":"A vertical machining centre."}""";

    public const string MetricCall = """
                                     {"machineId":"$machineId","key":"spindle.temperature",
                                     "name":"Spindle temperature",
                                     "valueKind":"numeric","quantityKind":"temperature",
                                     "unit":"celsius","min":0,"max":90,
                                     "warningHigh":70,"alarmHigh":80,"behavior":"level","sourceLocation":"page 4"}
                                     """;

    public const string StatusCall = """
                                     {"machineId":"$machineId","code":"E104","severity":"error",
                                     "description":"Spindle overheating.",
                                     "sourceLocation":"page 7"}
                                     """;

    public const string PhaseCall = """
                                    {"machineId":"$machineId","code":"cutting","name":"Cutting","priority":2,
                                    "isTracked":true,
                                    "sourceLocation":"page 9",
                                    "conditions":[
                                    {"metricKey":"spindle.temperature","operator":"greaterThan","value":30},
                                    {"statusCode":"E104","operator":"inactive"}],
                                    "limits":[{"metricKey":"spindle.temperature","warningHigh":75,"alarmHigh":85}]}
                                    """;

    public const string PhaseOfExistingCall = """
                                              {"machineId":"$machineId","code":"idle",
                                              "conditions":[
                                              {"metricKey":"spindle.load","operator":"lessThan","value":5},
                                              {"statusCode":"E50","operator":"inactive"}]}
                                              """;

    public const string PhaseWithUnknownKeyCall = """
                                                  {"machineId":"$machineId","code":"warmup",
                                                  "conditions":[{"metricKey":"no.such.key","operator":"active"}]}
                                                  """;

    public const string InvalidKeyMetricCall = """{"machineId":"$machineId","key":"Bad Key!"}""";

    public const string CorrectedMetricCall = """{"machineId":"$machineId","key":"coolant.level"}""";

    public static readonly Guid ExistingMetricId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000401");

    public static readonly Guid ExistingStatusId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000402");

    public static readonly Guid FileMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");

    public static readonly MachineSignals ExistingSignals = new()
    {
        Metrics = [new SignalReference { Id = ExistingMetricId, Key = "spindle.load" }],
        Statuses = [new SignalReference { Id = ExistingStatusId, Key = "E50" }],
    };
}
