namespace HealthyFac.Gateway.Web.Composition.Drafts;

/// <summary>
/// Defines the kind and state names of the draft grid as the services show them in JSON.
/// </summary>
public static class DraftNames
{
    public static class Kinds
    {
        public const string Machine = "machine";
        public const string Metric = "metric";
        public const string Status = "status";
        public const string Phase = "phase";
    }

    public static class States
    {
        public const string AwaitingTarget = "awaitingTarget";
    }

    public static class SortDirections
    {
        public const string Ascending = "asc";
        public const string Descending = "desc";
    }
}
