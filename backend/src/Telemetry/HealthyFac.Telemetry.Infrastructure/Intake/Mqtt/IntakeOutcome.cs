using HealthyFac.Shared.Application.Observability;
using HealthyFac.Telemetry.Application.Intake;

namespace HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;

/// <summary>
/// What the service did with one received message: the result for the span and the metrics, and whether the
/// service may acknowledge the message.
/// </summary>
public readonly record struct IntakeOutcome
{
    /// <summary>The result, from <see cref="ObservabilityConstants.Intake.Results"/>.</summary>
    public required string Result { get; init; }

    /// <summary>The envelope error code of a rejected message, else <see langword="null"/>.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>The sampling time of the message, or <see langword="null"/> when it is unreadable.</summary>
    public DateTime? SampledAt { get; init; }

    /// <summary>
    /// A message with the result <c>redelivery</c> stays unacknowledged, so the broker delivers it again. Any other
    /// result is final.
    /// </summary>
    public bool IsSettled => Result != ObservabilityConstants.Intake.Results.Redelivery;

    /// <summary>
    /// A stored message has readings, so it has a lag.
    /// </summary>
    public bool IsStored =>
        Result is ObservabilityConstants.Intake.Results.Stored
            or ObservabilityConstants.Intake.Results.StoredWithErrors;

    /// <summary>
    /// Maps the result of the use case to the result of the intake.
    /// </summary>
    /// <param name="response">The response of <see cref="ReceiveMachineMessageRequest"/>.</param>
    /// <returns>The outcome with the error code and the sampling time of the response.</returns>
    public static IntakeOutcome From(ReceiveMachineMessageResponse response) =>
        new()
        {
            Result = ToResult(response.Result),
            ErrorCode = response.ErrorCode,
            SampledAt = response.SampledAt,
        };

    private static string ToResult(ReceiveMachineMessageResult result) =>
        result switch
        {
            ReceiveMachineMessageResult.Stored => ObservabilityConstants.Intake.Results.Stored,
            ReceiveMachineMessageResult.StoredWithErrors => ObservabilityConstants.Intake.Results.StoredWithErrors,
            ReceiveMachineMessageResult.Rejected => ObservabilityConstants.Intake.Results.Rejected,
            ReceiveMachineMessageResult.Duplicate => ObservabilityConstants.Intake.Results.Duplicate,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, message: null),
        };

    /// <summary>
    /// A <c>failed</c> or <c>redelivery</c> message sets the status of its span to <c>Error</c>.
    /// </summary>
    public bool IsError =>
        Result is ObservabilityConstants.Intake.Results.Failed or ObservabilityConstants.Intake.Results.Redelivery;
}
