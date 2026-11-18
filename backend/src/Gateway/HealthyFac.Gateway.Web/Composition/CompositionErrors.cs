namespace HealthyFac.Gateway.Web.Composition;

/// <summary>
/// Defines the messages of the errors that the Gateway raises while it composes a response.
/// </summary>
public static class CompositionErrors
{
    public const string DownstreamUnavailable = "A service that the request needs is not available.";
    public const string SortValueMismatch = "A service returned a sort value of an unexpected kind.";
    public const string RowPositionMissing = "A service returned a row without a position.";
    public const string SortColumnUnknown = "The sort column is not one that the draft grid supports.";
    public const string BodyInvalid = "The request body is not a valid JSON object.";
    public const string LimitInvalid = "The limit must be a whole number above 0.";
    public const string SortInvalid = "The sort needs a known column and a direction of asc or desc.";
    public const string AfterInvalid = "The after position is not a position that a previous page returned.";
}
