using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HealthyFac.Insights.Application.Agents.Prompts;
using HealthyFac.Insights.Domain.Agents.Runs;

namespace HealthyFac.Insights.Infrastructure.Agents.Prompts;

/// <summary>
/// Reads the system prompts from embedded resources, so a change of the wording is a change of a file and not of
/// code. Each prompt has a version constant that the author raises with every change of the wording, and its SHA-256
/// hash. The run stores the version and the hash, never the text.
/// </summary>
public sealed class EmbeddedAgentPrompts : IAgentPrompts
{
    public const string TriageVersion = "triage-v1";
    public const string TriageResourceName = "HealthyFac.Insights.Infrastructure.Agents.Prompts.TriageSystemPrompt.md";
    public const string ChatTriageVersion = "chat-triage-v1";

    public const string ChatTriageResourceName =
        "HealthyFac.Insights.Infrastructure.Agents.Prompts.ChatTriageSystemPrompt.md";

    public const string ChatVersion = "chat-v1";
    public const string ChatResourceName = "HealthyFac.Insights.Infrastructure.Agents.Prompts.ChatSystemPrompt.md";
    public const string PlannerVersion = "planner-v4";
    public const string ScreenerVersion = "screener-v1";

    public const string ScreenerResourceName =
        "HealthyFac.Insights.Infrastructure.Agents.Prompts.ScreenerSystemPrompt.md";

    public const string PlannerResourceName =
        "HealthyFac.Insights.Infrastructure.Agents.Prompts.PlannerSystemPrompt.md";

    private const string MissingResourceMessage = "The agent prompt resource {0} was not found.";
    private const string UnknownRoleMessage = "The role {0} has no prompt.";

    private readonly Lazy<AgentPrompt> _triage = new(() => Read(TriageVersion, TriageResourceName));
    private readonly Lazy<AgentPrompt> _chat = new(() => Read(ChatVersion, ChatResourceName));
    private readonly Lazy<AgentPrompt> _chatTriage = new(() => Read(ChatTriageVersion, ChatTriageResourceName));
    private readonly Lazy<AgentPrompt> _planner = new(() => Read(PlannerVersion, PlannerResourceName));
    private readonly Lazy<AgentPrompt> _screener = new(() => Read(ScreenerVersion, ScreenerResourceName));

    public AgentPrompt For(AgentRole role)
    {
        if (role == AgentRole.Triage)
        {
            return _triage.Value;
        }

        if (role == AgentRole.Chat)
        {
            return _chat.Value;
        }

        if (role == AgentRole.Planner)
        {
            return _planner.Value;
        }

        return role == AgentRole.Screener
            ? _screener.Value
            : throw new InvalidOperationException(string.Format(UnknownRoleMessage, role.Value));
    }

    public AgentPrompt ForChatTriage() => _chatTriage.Value;

    private static AgentPrompt Read(string version, string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(string.Format(MissingResourceMessage, resourceName));

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var text = reader.ReadToEnd();

        return new AgentPrompt
        {
            Version = version,
            Hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text))),
            Text = text,
        };
    }
}
