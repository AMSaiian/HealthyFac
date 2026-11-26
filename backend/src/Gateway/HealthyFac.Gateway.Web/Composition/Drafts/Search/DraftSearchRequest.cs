using System.Text.Json;
using System.Text.Json.Nodes;

namespace HealthyFac.Gateway.Web.Composition.Drafts.Search;

/// <summary>
/// The parts of a composed draft search request that the Gateway needs for the merge. The services check the rest
/// (the range of the limit, the values of the filter).
/// </summary>
public sealed record DraftSearchRequest
{
    private const string LimitProperty = "limit";
    private const string SortProperty = "sort";
    private const string ColumnProperty = "column";
    private const string DirectionProperty = "direction";
    private const string FilterProperty = "filter";
    private const string AfterProperty = "after";

    public required int Limit { get; init; }

    public required string SortColumn { get; init; }

    public required bool IsDescending { get; init; }

    public JsonObject? Filter { get; init; }

    public ComposedPosition? After { get; init; }

    /// <summary>
    /// The sort that goes to both services. The Gateway always sends it, so Core and Telemetry never choose their own
    /// default and the merge sees one order.
    /// </summary>
    public JsonObject SortBody =>
        new()
        {
            [ColumnProperty] = SortColumn,
            [DirectionProperty] = IsDescending
                ? DraftNames.SortDirections.Descending
                : DraftNames.SortDirections.Ascending,
        };

    /// <summary>
    /// Reads the request body. A missing <c>sort</c> means the default: the pending drafts first.
    /// </summary>
    /// <param name="body">The JSON body of the request, or <see langword="null"/> when it was empty.</param>
    /// <param name="errors">The messages of each invalid field, empty when the body is valid.</param>
    /// <returns>The request, or <see langword="null"/> when a field is invalid.</returns>
    public static DraftSearchRequest? TryRead(JsonObject? body, out IReadOnlyDictionary<string, string[]> errors)
    {
        var found = new Dictionary<string, string[]>();
        errors = found;

        if (body is null)
        {
            found[string.Empty] = [CompositionErrors.BodyInvalid];

            return null;
        }

        var limit = ReadLimit(body[LimitProperty]);
        var (column, isDescending) = ReadSort(body[SortProperty]);
        var after = ReadAfter(body[AfterProperty], out var isAfterValid);

        if (limit is null)
        {
            found[LimitProperty] = [CompositionErrors.LimitInvalid];
        }

        if (column is null)
        {
            found[SortProperty] = [CompositionErrors.SortInvalid];
        }

        if (!isAfterValid)
        {
            found[AfterProperty] = [CompositionErrors.AfterInvalid];
        }

        return found.Count > 0
            ? null
            : new DraftSearchRequest
            {
                Limit = limit!.Value,
                SortColumn = column!,
                IsDescending = isDescending,
                Filter = body[FilterProperty] as JsonObject,
                After = after,
            };
    }

    private static int? ReadLimit(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<int>(out var limit) && limit > 0 ? limit : null;

    private static (string? Column, bool IsDescending) ReadSort(JsonNode? node)
    {
        if (node is null)
        {
            return (DraftSortComparers.State, false);
        }

        var column = (node as JsonObject)?[ColumnProperty] is JsonValue columnValue
            && columnValue.TryGetValue<string>(out var columnText)
            && (columnText == DraftSortComparers.State || columnText == DraftSortComparers.ProposedAt)
                ? columnText
                : null;

        var direction = (node as JsonObject)?[DirectionProperty] is JsonValue directionValue
            && directionValue.TryGetValue<string>(out var directionText)
                ? directionText
                : null;

        var isDirectionKnown = direction is DraftNames.SortDirections.Ascending
            or DraftNames.SortDirections.Descending;

        return (isDirectionKnown ? column : null, direction == DraftNames.SortDirections.Descending);
    }

    private static ComposedPosition? ReadAfter(JsonNode? node, out bool isValid)
    {
        isValid = true;

        if (node is null)
        {
            return null;
        }

        try
        {
            return node.Deserialize<ComposedPosition>(JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            isValid = false;

            return null;
        }
    }
}
