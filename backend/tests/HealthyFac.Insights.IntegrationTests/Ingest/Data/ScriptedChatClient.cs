using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.IntegrationTests.Ingest.Data;

/// <summary>
/// A chat client that answers from a script of texts. Without a script it answers with a valid extraction.
/// </summary>
public sealed class ScriptedChatClient : IChatClient
{
    public const string ValidAnswer = """
        {
          "machines": [
            {
              "name": "AX-200 Rotary Pump",
              "serialNumber": null,
              "description": "A rotary pump.",
              "metrics": [
                {
                  "key": "inlet.pressure", "name": "Inlet pressure", "description": null,
                  "valueKind": "numeric", "quantityKind": null, "unit": null,
                  "min": null, "max": 6, "states": null, "sourceLocation": "page 1"
                }
              ],
              "statuses": [
                { "code": "E12", "severity": "error", "description": "Overheating.", "sourceLocation": "page 2" }
              ]
            }
          ]
        }
        """;

    public const string InvalidAnswer = "This is not JSON.";

    private readonly Queue<string> _answers = new();

    public int CallCount { get; private set; }

    /// <summary>When set, every call throws it after it counts the call.</summary>
    public Exception? Failure { get; set; }

    /// <summary>The text of each call: the system prompt first, then the text of every message.</summary>
    public List<string> SentTexts { get; } = [];

    public void Script(params string[] answers)
    {
        foreach (var answer in answers)
        {
            _answers.Enqueue(answer);
        }
    }

    public void Reset()
    {
        _answers.Clear();
        Failure = null;
        SentTexts.Clear();
        CallCount = 0;
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        CallCount++;
        SentTexts.Add(string.Join(Environment.NewLine, [options?.Instructions, .. messages.Select(m => m.Text)]));
        if (Failure is not null)
        {
            throw Failure;
        }

        var answer = _answers.Count > 0 ? _answers.Dequeue() : ValidAnswer;

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, answer)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
