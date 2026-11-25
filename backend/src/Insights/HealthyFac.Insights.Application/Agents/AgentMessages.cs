namespace HealthyFac.Insights.Application.Agents;

/// <summary>
/// Defines the error messages of the agent settings.
/// </summary>
public static class AgentMessages
{
    public const string PlannerDecisionMessage =
        "Decide now with the evidence you have: call exactly one of the write tools.";

    public const string ValueNotPositive = "Every Insights:Agent value must be above 0.";
    public const string RunIdRequired = "RunId is required.";

    public const string ChatToolsNeedUserScopes =
        "The chat tools need the scopes of the user and never the advisor scopes.";

    public const string RunHasNoMachine = "The run has no machine.";
    public const string RunHasNoCase = "The run has no case.";
    public const string CompanyIdRequired = "CompanyId is required.";
    public const string MachineIdRequired = "MachineId is required.";
    public const string MetricKeyRequired = "MetricKey is required.";
    public const string UnknownMetricKey = "No active metric has this key on the machine of the case.";
    public const string WorkOrderIdRequired = "WorkOrderId is required.";
    public const string EpisodeIdsRequired = "At least one episode ID is required, and no ID is empty.";
    public const string QueryRequired = "The query is required and has one line.";
    public const string RangeStartNotBeforeEnd = "The start of the range must be before its end.";

    /// <summary>
    /// Builds the message of a history range that is longer than the option allows.
    /// </summary>
    /// <param name="maxHours">The longest range in hours.</param>
    /// <returns>The message for the model.</returns>
    public static string HistoryRangeTooLong(int maxHours) => $"The range must not be longer than {maxHours} hours.";
}
