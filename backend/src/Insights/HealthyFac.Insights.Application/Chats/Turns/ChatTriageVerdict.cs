using System.Text.Json;
using System.Text.Json.Serialization;

namespace HealthyFac.Insights.Application.Chats.Turns;

/// <summary>
/// The structured answer of the chat triage model. The model sees the response schema of this type, so
/// <see cref="ChatTriageChoice"/> lists the 3 verdicts as JSON strings.
/// </summary>
public sealed record ChatTriageVerdict
{
    [JsonConverter(typeof(ChatTriageChoiceConverter))]
    public required ChatTriageChoice Verdict { get; init; }
}

/// <summary>
/// The verdicts that the chat triage model may give. The JSON names are the camelCase names.
/// </summary>
public enum ChatTriageChoice
{
    InScope,
    OffTopic,
    Malicious,
}

/// <summary>
/// Reads and writes <see cref="ChatTriageChoice"/> as a camelCase string and rejects a number, so an answer outside
/// the schema ends the call.
/// </summary>
public sealed class ChatTriageChoiceConverter()
    : JsonStringEnumConverter<ChatTriageChoice>(JsonNamingPolicy.CamelCase, allowIntegerValues: false);
