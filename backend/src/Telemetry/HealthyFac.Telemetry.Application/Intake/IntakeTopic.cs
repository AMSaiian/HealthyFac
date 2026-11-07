using HealthyFac.Shared.Domain.Gateways;

namespace HealthyFac.Telemetry.Application.Intake;

/// <summary>
/// Reads the gateway ID from the topic of a machine message. The topic is the only source of the gateway ID.
/// </summary>
public static class IntakeTopic
{
    /// <summary>
    /// Takes the segment between the prefix and the suffix of the topic. It never throws.
    /// </summary>
    /// <param name="topic">The topic of the message.</param>
    /// <returns>The segment when the topic has the pattern and the segment has the gateway ID format; otherwise
    /// <see langword="null"/>.</returns>
    public static string? TryGetGatewayId(string topic)
    {
        if (!topic.StartsWith(IntakeConstants.TopicPrefix, StringComparison.Ordinal)
            || !topic.EndsWith(IntakeConstants.TopicSuffix, StringComparison.Ordinal))
        {
            return null;
        }

        var start = IntakeConstants.TopicPrefix.Length;
        var length = topic.Length - start - IntakeConstants.TopicSuffix.Length;

        if (length <= 0)
        {
            return null;
        }

        var segment = topic.Substring(start, length);

        return GatewayIds.IsValid(segment) ? segment : null;
    }
}
