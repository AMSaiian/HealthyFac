using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Domain.Cases.Signals;

/// <summary>
/// Defines how far the maintenance work covers a signal. <c>new</c> and <c>watched</c> are uncovered. The camelCase
/// values reach the API and the database, so never rename one in place.
/// </summary>
public sealed class SignalCoverage : Enumeration<SignalCoverage>
{
    public static readonly SignalCoverage New = new(value: "new");
    public static readonly SignalCoverage Watched = new(value: "watched");
    public static readonly SignalCoverage Dismissed = new(value: "dismissed");
    public static readonly SignalCoverage Drafted = new(value: "drafted");
    public static readonly SignalCoverage Linked = new(value: "linked");

    private SignalCoverage(string value)
        : base(value)
    {
    }

    /// <summary>Tells whether no work order, draft, or dismissal covers the signal yet.</summary>
    public bool IsUncovered => this == New || this == Watched;
}
