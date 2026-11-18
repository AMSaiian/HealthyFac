using System.Text.Json.Nodes;

namespace HealthyFac.Gateway.Web.Composition;

/// <summary>
/// The outcome of one downstream call: the JSON body, a client error with its problem details, or "unavailable".
/// </summary>
public abstract record DownstreamResult
{
    private DownstreamResult()
    {
    }

    /// <summary>The call succeeded (a <c>2xx</c> status). An empty body gives an empty JSON object.</summary>
    public sealed record Success : DownstreamResult
    {
        public required JsonNode Body { get; init; }
    }

    /// <summary>The service rejected the call with a <c>4xx</c> status. The problem is its JSON body.</summary>
    public sealed record ClientError : DownstreamResult
    {
        public required int Status { get; init; }

        public required JsonNode Problem { get; init; }
    }

    /// <summary>
    /// No destination is available, the connection failed, the call timed out, or the service answered <c>5xx</c> or
    /// an invalid body.
    /// </summary>
    public sealed record Unavailable : DownstreamResult;
}
