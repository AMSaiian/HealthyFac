using HealthyFac.Insights.Application.Extraction.Proposals;

namespace HealthyFac.Insights.Application.Chats.Drafts;

/// <summary>
/// The buffer of one existing machine that a chat turn proposes for. The container is the machine entry of the buffer:
/// it carries the update values of the machine, and the definitions of the machine hang on it.
/// </summary>
public sealed class ExistingMachine
{
    public required ExtractionBuffer Buffer { get; init; }

    /// <summary>The draft ID of the machine entry, or <see langword="null"/> before the first call.</summary>
    public Guid? ContainerId { get; set; }
}
