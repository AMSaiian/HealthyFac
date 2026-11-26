using System.Text.RegularExpressions;

namespace HealthyFac.Insights.Application.Agents.Prompts;

/// <summary>
/// Wraps text that comes from data (a case, a tool result, a document) in a tagged block. The text cannot close or
/// open its own block, because the tags of that name are escaped. The system prompt tells the model that a block
/// holds data and no instructions.
/// </summary>
public static class AgentDataBlocks
{
    private static readonly TimeSpan PatternMatchTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Wraps the text in <c>&lt;tag&gt;</c> and <c>&lt;/tag&gt;</c>.
    /// </summary>
    /// <param name="tag">The name of the block, in lower case letters.</param>
    /// <param name="text">The data.</param>
    /// <returns>The block.</returns>
    public static string Wrap(string tag, string text)
    {
        var escaped = TagPattern(tag)
            .Replace(
                text,
                match => match.Value.Replace(oldValue: "<", newValue: "&lt;").Replace(oldValue: ">", newValue: "&gt;"));

        return $"<{tag}>{Environment.NewLine}{escaped}{Environment.NewLine}</{tag}>";
    }

    private static Regex TagPattern(string tag) =>
        new(
            $"</?{Regex.Escape(tag)}>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            PatternMatchTimeout);
}
