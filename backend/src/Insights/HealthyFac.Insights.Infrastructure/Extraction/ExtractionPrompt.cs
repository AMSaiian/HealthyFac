using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HealthyFac.Insights.Application.Extraction;
using HealthyFac.Insights.Application.MetricUnits;

namespace HealthyFac.Insights.Infrastructure.Extraction;

/// <summary>
/// Builds the system prompt and the user message of the extraction call. The prompt text is an embedded resource, so a
/// change of the wording is a change of a file and not of code.
/// </summary>
public static partial class ExtractionPrompt
{
    public const string ResourceName =
        "HealthyFac.Insights.Infrastructure.Extraction.Prompts.ExtractionSystemPrompt.md";

    public const string UnitCatalogPlaceholder = "{{unit_catalog}}";
    public const string ExistingKeysPlaceholder = "{{existing_metric_keys}}";
    public const string ExistingCodesPlaceholder = "{{existing_status_codes}}";
    public const string MachineFileLine = "The document belongs to one machine. Return exactly 1 machine.";

    private const string NoneText = "(none)";
    private const string UnitSeparator = " | ";
    private const string DocumentStart = "<document>";
    private const string DocumentEnd = "</document>";
    private const string UnresolvedMessage = "The extraction prompt still holds the placeholder {0}.";
    private const string MissingResourceMessage = "The extraction prompt resource {0} was not found.";

    /// <summary>
    /// Reads the system prompt resource and fills the three placeholders.
    /// </summary>
    /// <param name="input">Holds the unit catalog and the existing keys and codes.</param>
    /// <returns>The system prompt.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the resource is missing, or when a placeholder stays in the text.
    /// </exception>
    public static string BuildSystemPrompt(ExtractionInput input)
    {
        var template = ReadTemplate();
        var prompt = template
            .Replace(UnitCatalogPlaceholder, FormatUnits(input.Units), StringComparison.Ordinal)
            .Replace(ExistingKeysPlaceholder, FormatLines(input.ExistingMetricKeys), StringComparison.Ordinal)
            .Replace(ExistingCodesPlaceholder, FormatLines(input.ExistingStatusCodes), StringComparison.Ordinal);

        var left = PlaceholderPattern().Match(prompt);
        if (left.Success)
        {
            throw new InvalidOperationException(string.Format(UnresolvedMessage, left.Value));
        }

        return prompt;
    }

    /// <summary>
    /// Builds the user message: an optional line for a machine file, then the document with one block for each chunk.
    /// The text of a chunk cannot close or open the document block, because its tags are escaped.
    /// </summary>
    /// <param name="input">Holds the chunks and the machine file flag.</param>
    /// <returns>The user message.</returns>
    public static string BuildUserMessage(ExtractionInput input)
    {
        var builder = new StringBuilder();
        if (input.IsMachineFile)
        {
            builder.AppendLine(MachineFileLine);
        }

        builder.AppendLine(DocumentStart);
        foreach (var chunk in input.Chunks)
        {
            var location = string.IsNullOrWhiteSpace(chunk.Location) ? string.Empty : $"[{Escape(chunk.Location)}] ";
            builder.AppendLine($"{location}{Escape(chunk.Text)}");
        }

        builder.Append(DocumentEnd);

        return builder.ToString();
    }

    private static string Escape(string text) =>
        DocumentTagPattern().Replace(text, match => match.Value.Replace("<", "&lt;").Replace(">", "&gt;"));

    private static string FormatUnits(IReadOnlyList<MetricUnit> units)
    {
        var lines = units
            .Where(unit => !unit.IsDiscrete)
            .Select(unit => string.Join(UnitSeparator, unit.QuantityKind, unit.Name, unit.Abbreviation))
            .ToList();

        return lines.Count == 0 ? NoneText : string.Join(Environment.NewLine, lines);
    }

    private static string FormatLines(IReadOnlyList<string> values) =>
        values.Count == 0 ? NoneText : string.Join(Environment.NewLine, values);

    private static string ReadTemplate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(string.Format(MissingResourceMessage, ResourceName));
        using var reader = new StreamReader(stream, Encoding.UTF8);

        return reader.ReadToEnd();
    }

    [GeneratedRegex(@"\{\{[a-z_]+\}\}")]
    private static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"</?document>", RegexOptions.IgnoreCase)]
    private static partial Regex DocumentTagPattern();
}
