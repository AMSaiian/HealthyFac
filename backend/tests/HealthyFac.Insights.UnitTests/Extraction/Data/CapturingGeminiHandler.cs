using System.Net;
using System.Text;

namespace HealthyFac.Insights.UnitTests.Extraction.Data;

/// <summary>
/// Plays the Gemini API: it records the body of each request and answers with a fixed candidate text.
/// </summary>
public sealed class CapturingGeminiHandler : HttpMessageHandler, IHttpClientFactory
{
    private readonly string _answerText;

    public CapturingGeminiHandler(string answerText)
    {
        _answerText = answerText;
    }

    public List<string> Bodies { get; } = [];

    public List<string> Paths { get; } = [];

    public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        Paths.Add(request.RequestUri!.AbsolutePath);
        Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

        var escaped = _answerText.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        var json = $$"""
            {
              "candidates": [
                { "content": { "role": "model", "parts": [ { "text": "{{escaped}}" } ] },
                  "finishReason": "STOP", "index": 0 }
              ],
              "usageMetadata": { "promptTokenCount": 1, "candidatesTokenCount": 1, "totalTokenCount": 2 }
            }
            """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }
}
