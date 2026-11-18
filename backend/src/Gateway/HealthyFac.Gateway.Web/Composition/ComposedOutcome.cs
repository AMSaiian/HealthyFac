using System.Text.Json.Nodes;

namespace HealthyFac.Gateway.Web.Composition;

/// <summary>
/// The combined outcome of the downstream calls of one composed endpoint: every body, or the one failure that the
/// Gateway answers.
/// </summary>
public abstract record ComposedOutcome
{
    private ComposedOutcome()
    {
    }

    /// <summary>Every call succeeded. The bodies keep the order of the calls.</summary>
    public sealed record Success : ComposedOutcome
    {
        public required IReadOnlyList<JsonNode> Bodies { get; init; }
    }

    /// <summary>A call failed. The Gateway answers with this status and this problem details body.</summary>
    public sealed record Failure : ComposedOutcome
    {
        public required int Status { get; init; }

        public required JsonNode Problem { get; init; }
    }
}
