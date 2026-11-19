namespace HealthyFac.Insights.Web.Cases;

/// <summary>The page of a case child list (the signals or the runs of a case).</summary>
public record SearchCaseChildrenRequest
{
    public required int Offset { get; init; }

    public required int Limit { get; init; }
}
