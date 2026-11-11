using System.Net;
using System.Text;

namespace HealthyFac.Insights.UnitTests.Conversion.Data;

/// <summary>
/// Plays docling-serve for the converter tests: it accepts a submit, answers each poll from a script of task statuses,
/// and serves one result. It records every call.
/// </summary>
public sealed class FakeDoclingServer : HttpMessageHandler
{
    public const string TaskId = "task-1";

    private readonly Queue<string> _statuses = new();
    private readonly List<RecordedDoclingCall> _calls = [];

    public string ResultJson { get; set; } = "{}";

    public string? ErrorMessage { get; set; }

    /// <summary>The next submits that answer 503.</summary>
    public int SubmitFailures { get; set; }

    /// <summary>The next polls that answer 503.</summary>
    public int PollFailures { get; set; }

    /// <summary>The next reads of the result that answer 503.</summary>
    public int ResultFailures { get; set; }

    public IReadOnlyList<RecordedDoclingCall> Calls => _calls;

    public void ScriptStatuses(params string[] statuses)
    {
        foreach (var status in statuses)
        {
            _statuses.Enqueue(status);
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var body = request.Content is null
            ? string.Empty
            : Encoding.UTF8.GetString(await request.Content.ReadAsByteArrayAsync(cancellationToken));
        var apiKey = request.Headers.TryGetValues(name: "X-Api-Key", out var values) ? values.Single() : null;
        _calls.Add(new RecordedDoclingCall(request.Method.Method, request.RequestUri!.AbsolutePath, apiKey, body));

        var path = request.RequestUri.AbsolutePath;

        return path switch
        {
            "/v1/chunk/hybrid/file/async" => FailOr(
                () => SubmitFailures,
                () => SubmitFailures--,
                () => Json($$"""{"task_id":"{{TaskId}}","task_status":"pending"}""")),
            $"/v1/status/poll/{TaskId}" => FailOr(() => PollFailures, () => PollFailures--, () => Json(NextStatus())),
            $"/v1/result/{TaskId}" => FailOr(() => ResultFailures, () => ResultFailures--, () => Json(ResultJson)),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        };
    }

    private static HttpResponseMessage FailOr(
        Func<int> remaining,
        Func<int> consume,
        Func<HttpResponseMessage> answer
    )
    {
        if (remaining() <= 0)
        {
            return answer();
        }

        consume();

        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, mediaType: "application/json") };

    private string NextStatus()
    {
        var status = _statuses.Count > 0 ? _statuses.Dequeue() : "success";
        var message = ErrorMessage is null ? "null" : $"\"{ErrorMessage}\"";

        return $$"""{"task_id":"{{TaskId}}","task_status":"{{status}}","error_message":{{message}}}""";
    }
}

/// <summary>
/// One recorded call to the fake docling-serve.
/// </summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The absolute path.</param>
/// <param name="ApiKey">The value of the <c>X-Api-Key</c> header, or <see langword="null"/>.</param>
/// <param name="Body">The request body as text.</param>
public sealed record RecordedDoclingCall(string Method, string Path, string? ApiKey, string Body);
