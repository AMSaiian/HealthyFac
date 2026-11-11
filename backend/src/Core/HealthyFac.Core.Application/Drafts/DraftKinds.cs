namespace HealthyFac.Core.Application.Drafts;

/// <summary>
/// Defines the kinds of draft that the draft grid and the API show. The camelCase values reach the API.
/// </summary>
public static class DraftKinds
{
    public const string Machine = "machine";
    public const string Metric = "metric";
    public const string Status = "status";

    public static readonly string[] All = [Machine, Metric, Status];
}
