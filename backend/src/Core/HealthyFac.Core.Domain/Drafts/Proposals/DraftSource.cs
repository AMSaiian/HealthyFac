using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Domain.Drafts.Proposals;

/// <summary>
/// Defines where a draft proposal came from. The camelCase values reach the API and the database, so never rename one
/// in place.
/// </summary>
public sealed class DraftSource : Enumeration<DraftSource>
{
    /// <summary>The ingest of a file proposed the drafts.</summary>
    public static readonly DraftSource Ingest = new(value: "ingest");

    /// <summary>The planner agent of a case proposed the drafts.</summary>
    public static readonly DraftSource Case = new(value: "case");

    /// <summary>The chat bot proposed the drafts.</summary>
    public static readonly DraftSource Chat = new(value: "chat");

    private DraftSource(string value)
        : base(value)
    {
    }
}
