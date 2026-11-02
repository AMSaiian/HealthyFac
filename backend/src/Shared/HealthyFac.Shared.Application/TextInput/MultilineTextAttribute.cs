namespace HealthyFac.Shared.Application.TextInput;

/// <summary>
/// Marks a string property of a request or a message as multi-line text. The input keeps its inner line breaks and
/// tabs, gets <c>\n</c> line endings, and loses the outer whitespace. Other control characters are rejected.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MultilineTextAttribute : Attribute;
