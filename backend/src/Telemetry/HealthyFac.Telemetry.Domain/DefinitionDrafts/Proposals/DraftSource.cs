using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;

/// <summary>
/// Defines where a draft proposal came from. The camelCase values reach the API and the database, so never rename one
/// in place.
/// </summary>
public sealed class DraftSource : Enumeration<DraftSource>
{
    /// <summary>The ingest of a file proposed the drafts.</summary>
    public static readonly DraftSource Ingest = new(value: "ingest");

    /// <summary>The chat bot proposed the drafts.</summary>
    public static readonly DraftSource Chat = new(value: "chat");

    private DraftSource(string value)
        : base(value)
    {
    }
}
