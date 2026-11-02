using System.Text.Json;
using HealthyFac.Shared.Infrastructure.TextInput;

namespace HealthyFac.Shared.Infrastructure.Messaging;

/// <summary>
/// Holds the one JSON options instance of the message payloads: the web defaults (camelCase names) plus the text
/// input normalization, so a consumed message reads its strings as an HTTP request does. The outbox writes with it
/// and the inbox dispatcher reads with it.
/// </summary>
public static class MessagingJsonOptions
{
    public static readonly JsonSerializerOptions Instance = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions.Web);
        TextInputJson.Configure(options);
        options.MakeReadOnly();

        return options;
    }
}
