using System.Diagnostics;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using MQTTnet;
using MQTTnet.Packets;
using Tags = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Intake.Tags;

namespace HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;

/// <summary>
/// Starts and completes the consumer span of one received message. The span name holds the subscription pattern, never
/// the gateway ID. The tags follow the OpenTelemetry messaging conventions, as the spans of the RabbitMQ pipeline do.
/// </summary>
internal static class IntakeActivity
{
    private static readonly ActivitySource Source = new(ObservabilityConstants.Intake.Source);

    /// <summary>
    /// Starts the consumer span. A valid <c>traceparent</c> user property of the message is the remote parent, so
    /// the span joins the trace of the sender. A missing or invalid value starts a new trace; the callers run in
    /// background workers, which have no ambient activity. The method never throws for a bad value.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <param name="sharedGroup">The shared subscription group.</param>
    /// <param name="consumerIndex">The index of the consumer that received the message.</param>
    /// <returns>The started span, or <see langword="null"/> while no listener records the source.</returns>
    public static Activity? Start(MqttApplicationMessage message, string sharedGroup, int consumerIndex)
    {
        var traceParent = message.UserProperties
            ?.FirstOrDefault(property => property.Name == MessageHeaders.TraceParent)
            ?.ReadValueAsString();
        var name = ObservabilityConstants.Intake.SpanName;
        var activity = ActivityContext.TryParse(traceParent, traceState: null, isRemote: true, out var parent)
            ? Source.StartActivity(name, ActivityKind.Consumer, parent)
            : Source.StartActivity(name, ActivityKind.Consumer);

        return activity
            .WithTag(ObservabilityConstants.Messaging.Tags.SystemName, ObservabilityConstants.Intake.MqttSystem)
            .WithTag(
                ObservabilityConstants.Messaging.Tags.OperationType,
                ObservabilityConstants.Messaging.Operations.Process)
            .WithTag(ObservabilityConstants.Messaging.Tags.DestinationName, message.Topic)
            .WithTag(Tags.ConsumerGroup, sharedGroup)
            .WithTag(Tags.ConsumerIndex, consumerIndex);
    }

    /// <summary>
    /// Sets the result tags of the span, and the status <c>Error</c> for a failed or redelivered message.
    /// </summary>
    /// <param name="activity">The span, or <see langword="null"/>.</param>
    /// <param name="outcome">The result of the message.</param>
    public static void Complete(Activity? activity, IntakeOutcome outcome)
    {
        activity
            .WithTag(Tags.Result, outcome.Result)
            .WithTag(Tags.ErrorCode, outcome.ErrorCode);

        if (outcome.IsError)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
        }
    }
}
