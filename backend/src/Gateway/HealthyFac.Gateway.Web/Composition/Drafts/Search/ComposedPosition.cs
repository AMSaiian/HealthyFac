namespace HealthyFac.Gateway.Web.Composition.Drafts.Search;

/// <summary>
/// The position of a composed draft page: the keyset position of each source, and whether the source has no more rows.
/// The web app sends it back unchanged as <c>after</c>.
/// </summary>
public sealed record ComposedPosition
{
    /// <summary>The position of the last Core row that was read, or <see langword="null"/> at the start.</summary>
    public KeysetPositionDto? Core { get; init; }

    /// <summary>The position of the last Telemetry row that was read, or <see langword="null"/> at the start.</summary>
    public KeysetPositionDto? Telemetry { get; init; }

    public bool IsCoreFinished { get; init; }

    public bool IsTelemetryFinished { get; init; }
}
