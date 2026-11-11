using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// The result of the mapping: the message for Core, the number of drafts in it, and the warning for the user.
/// </summary>
public record MappingResult
{
    /// <summary>The message, or <see langword="null"/> when the extraction holds nothing to propose.</summary>
    public DraftsProposed? Message { get; init; }

    /// <summary>The number of machine, metric, and status drafts in <see cref="Message"/>.</summary>
    public required int DraftCount { get; init; }

    /// <summary>A translation key with values (JSON) for a partial result, or <see langword="null"/>.</summary>
    public string? Warning { get; init; }
}
