using System.Diagnostics;
using HealthyFac.Shared.Application.Observability;

namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Starts the spans of the messaging pipeline. Every span has the same name form and the same tags, so the
/// publish, the receive, and the processing of one message read alike in a trace.
/// </summary>
internal static class MessagingActivityExtensions
{
    extension(ActivitySource source)
    {
        /// <summary>
        /// Starts a messaging span. A valid <paramref name="traceParent"/> is the remote parent, so the span joins
        /// the trace of the work that created the message. A missing or invalid value starts a new trace; the
        /// callers run in background workers, which have no ambient activity. The method never throws for a bad
        /// value.
        /// </summary>
        /// <param name="operation">The OpenTelemetry operation type, from
        /// <see cref="ObservabilityConstants.Messaging.Operations"/>.</param>
        /// <param name="kind">The kind of the span: producer for a send, consumer for a receive or a
        /// process.</param>
        /// <param name="messageType">The stable contract name of the message.</param>
        /// <param name="messageId">The message ID.</param>
        /// <param name="traceParent">The W3C <c>traceparent</c> value of the parent, or <see langword="null"/>.
        /// </param>
        /// <returns>The started span, or <see langword="null"/> while no listener records the source.</returns>
        public Activity? StartMessagingActivity(
            string operation,
            ActivityKind kind,
            string messageType,
            string messageId,
            string? traceParent
        )
        {
            var name = $"{operation} {messageType}";
            var activity = ActivityContext.TryParse(traceParent, null, isRemote: true, out var parent)
                ? source.StartActivity(name, kind, parent)
                : source.StartActivity(name, kind);

            return activity
                .WithTag(ObservabilityConstants.Messaging.Tags.SystemName, ObservabilityConstants.Messaging.RabbitMq)
                .WithTag(ObservabilityConstants.Messaging.Tags.OperationType, operation)
                .WithTag(ObservabilityConstants.Messaging.Tags.DestinationName, messageType)
                .WithTag(ObservabilityConstants.Messaging.Tags.MessageId, messageId);
        }
    }
}
