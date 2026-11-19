using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.IntegrationTests.Ingest.Data;

/// <summary>
/// A chat client that answers from a script of responses. Without a script it plays a valid extraction: it calls
/// the machine tool, then the metric tool and the status tool, then ends with a text. A scripted tool call can use
/// <see cref="MachineIdToken"/> as a value, and the client replaces it with the <c>machineId</c> of the last tool
/// result.
/// </summary>
public sealed class ScriptedChatClient : IChatClient
{
    public const string DefaultMachineName = "AX-200 Rotary Pump";
    public const string DefaultMetricKey = "inlet.pressure";
    public const string DefaultStatusCode = "E12";
    public const string MachineIdToken = "$machineId";

    private const string DefaultText = "Done.";
    private const string MachineIdPattern = @"""machineId""\s*:\s*""([^""]+)""";

    private readonly Queue<ChatResponse> _responses = new();

    public int CallCount { get; private set; }

    /// <summary>The token counts that every answer reports, or <see langword="null"/> for none.</summary>
    public UsageDetails? Usage { get; set; }

    /// <summary>Runs inside each call before it answers. A test uses it to let time pass during the call.</summary>
    public Func<Task>? Before { get; set; }

    /// <summary>
    /// When set, the call with the number <see cref="OnceFailureCall"/> throws it, and no other call does.
    /// </summary>
    public Exception? OnceFailure { get; set; }

    public int OnceFailureCall { get; set; }

    /// <summary>The number of messages that each call received, in the order of the calls.</summary>
    public List<int> SentMessageCounts { get; } = [];

    /// <summary>When set, every call throws it after it counts the call.</summary>
    public Exception? Failure { get; set; }

    /// <summary>The options of each call: the tools and the tool mode that the call offered.</summary>
    public List<ChatOptions?> SentOptions { get; } = [];

    /// <summary>The text of each call: the system prompt first, then the text of every message.</summary>
    public List<string> SentTexts { get; } = [];

    /// <summary>
    /// Queues an answer that asks for one tool call. The next call of the model gets the next answer.
    /// </summary>
    public void ScriptToolCall(string callId, string functionName, string argumentsJson) =>
        _responses.Enqueue(CallsResponse((callId, functionName, argumentsJson)));

    /// <summary>
    /// Queues one answer that asks for several tool calls. The function loop runs them in order.
    /// </summary>
    /// <param name="calls">The call ID, the tool name, and the arguments as JSON of each call.</param>
    public void ScriptToolCalls(params (string CallId, string Name, string ArgumentsJson)[] calls) =>
        _responses.Enqueue(CallsResponse(calls));

    /// <summary>
    /// Queues tool calls that the tool rejects (the <c>machineId</c> is not an encrypted ID).
    /// </summary>
    /// <param name="count">The number of calls, one for each model turn.</param>
    public void ScriptInvalidToolCalls(int count)
    {
        for (var index = 0; index < count; index++)
        {
            ScriptToolCall(
                $"invalid-{index}", functionName: "propose_metric", argumentsJson: """{"machineId":"not-an-id"}""");
        }
    }

    /// <summary>Queues a final text answer for each text.</summary>
    public void Script(params string[] answers)
    {
        foreach (var answer in answers)
        {
            ScriptText(answer);
        }
    }

    /// <summary>Queues a final text answer.</summary>
    public void ScriptText(string text) =>
        _responses.Enqueue(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));

    public void Reset()
    {
        _responses.Clear();
        Failure = null;
        OnceFailure = null;
        OnceFailureCall = 0;
        Before = null;
        Usage = null;
        SentTexts.Clear();
        SentOptions.Clear();
        CallCount = 0;
        SentMessageCounts.Clear();
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        CallCount++;
        var sentMessages = messages.ToList();
        SentMessageCounts.Add(sentMessages.Count);
        SentOptions.Add(options);
        SentTexts.Add(string.Join(Environment.NewLine, [options?.Instructions, .. messages.Select(m => m.Text)]));
        if (Failure is not null)
        {
            throw Failure;
        }

        if (OnceFailure is not null && CallCount == OnceFailureCall)
        {
            throw OnceFailure;
        }

        if (Before is not null)
        {
            await Before();
        }

        if (_responses.Count > 0)
        {
            var scripted = _responses.Dequeue();
            ReplaceMachineId(scripted, FindMachineId(sentMessages));
            scripted.Usage = Usage;

            return scripted;
        }

        var response = PlayDefault(sentMessages);
        response.Usage = Usage;

        return response;
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

    private static void ReplaceMachineId(ChatResponse response, string? machineId)
    {
        foreach (var call in response.Messages.SelectMany(message => message.Contents).OfType<FunctionCallContent>())
        {
            var arguments = call.Arguments;
            if (arguments is null || machineId is null)
            {
                continue;
            }

            foreach (var key in arguments.Where(pair => pair.Value?.ToString() == MachineIdToken)
                .Select(pair => pair.Key)
                .ToList())
            {
                arguments[key] = machineId;
            }
        }
    }

    private static ChatResponse CallsResponse(params (string CallId, string Name, string ArgumentsJson)[] calls) =>
        new(
            new ChatMessage(
                ChatRole.Assistant,
                [
                    .. calls.Select(call => (AIContent)new FunctionCallContent(
                        call.CallId,
                        call.Name,
                        System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(call.ArgumentsJson))),
                ]));

    private static string? FindMachineId(IReadOnlyList<ChatMessage> messages) =>
        messages.SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>()
            .Select(result => System.Text.RegularExpressions.Regex.Match(
                result.Result?.ToString() ?? string.Empty, MachineIdPattern))
            .LastOrDefault(match => match.Success)
            ?.Groups[groupnum: 1]
            .Value;

    private static ChatResponse PlayDefault(IReadOnlyList<ChatMessage> messages)
    {
        var resultCount = messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Count();

        return resultCount switch
        {
            0 => CallsResponse(
                (
                    "machine-1",
                    "propose_machine",
                    $$"""{"name":"{{DefaultMachineName}}","description":"A rotary pump."}""")),
            1 => CallsResponse(
                (
                    "metric-1",
                    "propose_metric",
                    $$"""
                      {"machineId":"{{FindMachineId(messages)}}","key":"{{DefaultMetricKey}}","name":"Inlet pressure",
                      "valueKind":"numeric","max":6,"sourceLocation":"page 1"}
                      """),
                (
                    "status-1",
                    "propose_status",
                    $$"""
                      {"machineId":"{{FindMachineId(messages)}}","code":"{{DefaultStatusCode}}","severity":"error",
                      "description":"Overheating.","sourceLocation":"page 2"}
                      """)),
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, DefaultText)),
        };
    }
}
