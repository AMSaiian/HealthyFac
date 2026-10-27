using System.Diagnostics;
using HealthyFac.Shared.Application.Observability;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class MessagingSpanExtensions
{
    extension(Activity span)
    {
        public string? ReadMessageId() =>
            span.GetTagItem(ObservabilityConstants.Messaging.Tags.MessageId)?.ToString();

        public bool IsOperation(string operation) =>
            span.DisplayName.StartsWith($"{operation} ", StringComparison.Ordinal);
    }
}
