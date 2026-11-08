using HealthyFac.Shared.Application.Observability;
using HealthyFac.Telemetry.Domain.Intake;

namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

public static class IntakeObservabilityTestData
{
    private const int LongTopicSegmentLength = 1100;

    public static readonly TimeSpan SlowSaveDelay = TimeSpan.FromSeconds(seconds: 3);

    public static readonly TimeSpan Lag = TimeSpan.FromMinutes(minutes: 5);

    public static readonly string LongGatewayId = new('a', LongTopicSegmentLength);

    public static readonly HashSet<string> AllowedTagKeys =
    [
        ObservabilityConstants.Intake.Tags.Result,
        ObservabilityConstants.Intake.Tags.ErrorCode,
    ];

    public static string EnvelopeFailureJson => """{"metrics":{},"statuses":[]}""";

    public static TheoryData<IntakeResultCase> Results => new()
    {
        new IntakeResultCase { Result = ObservabilityConstants.Intake.Results.Stored },
        new IntakeResultCase
        {
            Result = ObservabilityConstants.Intake.Results.StoredWithErrors,
            Json = ReceiveMachineMessageTestData.CreateMessage(temperature: "hot").ToJsonString(),
        },
        new IntakeResultCase
        {
            Result = ObservabilityConstants.Intake.Results.Rejected,
            ErrorCode = IntakeErrorCodes.MissingField,
            Json = EnvelopeFailureJson,
        },
        new IntakeResultCase { Result = ObservabilityConstants.Intake.Results.Duplicate, IsRepeated = true },
        new IntakeResultCase { Result = ObservabilityConstants.Intake.Results.Invalid, IsLongTopic = true },
        new IntakeResultCase { Result = ObservabilityConstants.Intake.Results.Failed, IsFailureArmed = true },
    };
}
