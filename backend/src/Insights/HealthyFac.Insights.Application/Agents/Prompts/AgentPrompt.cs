namespace HealthyFac.Insights.Application.Agents.Prompts;

/// <summary>
/// The system prompt of an agent role. The run stores the version and the hash, never the text.
/// </summary>
public sealed record AgentPrompt
{
    /// <summary>The version of the wording, for example <c>triage-v1</c>.</summary>
    public required string Version { get; init; }

    /// <summary>The SHA-256 of the prompt text as lower case hex.</summary>
    public required string Hash { get; init; }

    public required string Text { get; init; }
}
