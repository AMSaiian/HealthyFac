using System.Text.Json.Nodes;

namespace HealthyFac.Gateway.Web.Composition.Drafts.Search;

/// <summary>The filter that one source gets, or the decision to skip the source.</summary>
public sealed record SourceFilter
{
    /// <summary>Whether the source cannot hold a matching row, so the Gateway makes no call.</summary>
    public required bool IsSkipped { get; init; }

    /// <summary>The filter to send, or <see langword="null"/> when the request had none.</summary>
    public JsonObject? Filter { get; init; }
}

/// <summary>The filters of the two sources of the draft grid.</summary>
public sealed record DraftFilterSplit
{
    public required SourceFilter Core { get; init; }

    public required SourceFilter Telemetry { get; init; }
}

/// <summary>
/// Splits the filter of a composed draft request into one filter for Core and one for Telemetry. Core holds only
/// machine drafts, in the states pending, applied, and dismissed. Telemetry holds metric, status, and phase drafts in
/// all 4
/// states. A value that no source knows goes to both, so each service answers <c>400</c> as it does for its own grid.
/// </summary>
public static class DraftFilterSplitter
{
    private const string KindsProperty = "kinds";
    private const string StatesProperty = "states";
    private const string ValuesProperty = "values";

    private static readonly string[] TelemetryOnlyKinds =
        [DraftNames.Kinds.Metric, DraftNames.Kinds.Status, DraftNames.Kinds.Phase];

    private static readonly string[] CoreOnlyKinds = [DraftNames.Kinds.Machine];
    private static readonly string[] TelemetryOnlyStates = [DraftNames.States.AwaitingTarget];

    /// <summary>
    /// Splits the filter.
    /// </summary>
    /// <param name="filter">The <c>filter</c> object of the request, or <see langword="null"/> for none.</param>
    /// <returns>The filter of each source. The input object stays unchanged.</returns>
    public static DraftFilterSplit Split(JsonObject? filter) =>
        new()
        {
            Core = SplitFor(filter, TelemetryOnlyKinds, TelemetryOnlyStates),
            Telemetry = SplitFor(filter, CoreOnlyKinds, []),
        };

    private static SourceFilter SplitFor(JsonObject? filter, string[] excludedKinds, string[] excludedStates)
    {
        if (filter is null)
        {
            return new SourceFilter { IsSkipped = false };
        }

        var copy = (JsonObject)filter.DeepClone();
        var isKindsEmpty = Narrow(copy, KindsProperty, excludedKinds);
        var isStatesEmpty = Narrow(copy, StatesProperty, excludedStates);

        return new SourceFilter { IsSkipped = isKindsEmpty || isStatesEmpty, Filter = copy };
    }

    /// <summary>
    /// Removes the excluded values of one set filter. A value that is not a string is not excluded, so it goes to both
    /// sources and each service answers <c>400</c>.
    /// </summary>
    private static bool Narrow(JsonObject filter, string property, string[] excludedValues)
    {
        if (filter[property] is not JsonObject set || set[ValuesProperty] is not JsonArray values)
        {
            return false;
        }

        var kept = values
            .Where(value => !IsExcluded(value, excludedValues))
            .Select(value => value?.DeepClone())
            .ToList();

        set[ValuesProperty] = new JsonArray([.. kept]);

        return kept.Count == 0;
    }

    private static bool IsExcluded(JsonNode? value, string[] excludedValues) =>
        value is JsonValue text && text.TryGetValue<string>(out var name) && excludedValues.Contains(name);
}
