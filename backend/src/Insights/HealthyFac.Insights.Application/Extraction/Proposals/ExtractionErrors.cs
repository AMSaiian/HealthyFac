using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Insights.Application.Extraction.Proposals;

/// <summary>
/// Defines the rejections that an extractor tool sends back to the model.
/// </summary>
public static class ExtractionErrors
{
    public static readonly string MachineFileHasOneMachine =
        "The document belongs to one machine. Propose exactly 1 machine.";

    public static readonly string TooManyMachines =
        $"A proposal holds at most {DraftMessageLimits.MaxMachines} machines.";

    public static readonly string TooManyMetrics =
        $"A machine holds at most {DraftMessageLimits.MaxMetrics} metrics.";

    public static readonly string TooManyStatuses =
        $"A machine holds at most {DraftMessageLimits.MaxStatuses} statuses.";

    public static readonly string RangeNeedsNumeric = "Min and Max need the value kind numeric.";

    public static readonly string StatesNeedStateSet = "States need the value kind stateSet.";

    public static readonly string UnitNeedsBothValues = "Send quantityKind and unit together, or send both null.";

    public static readonly string RunHasNoBuffer = "The run does not collect proposals.";

    public static UnprocessableException UnknownMachine() =>
        new(message: "The machineId is not the ID that propose_machine returned in this run.");

    public static UnprocessableException DuplicateMetricKey(string key) =>
        new($"The metric key '{key}' is already proposed for this machine.");

    public static UnprocessableException DuplicateStatusCode(string code) =>
        new($"The status code '{code}' is already proposed for this machine.");

    public static UnprocessableException DuplicatePhaseCode(string code) =>
        new($"The phase code '{code}' is already proposed for this machine.");

    public static UnprocessableException UnknownUnit() =>
        new(message: "The quantityKind and unit pair is not in the unit catalog. Use a listed pair or send both null.");

    public static UnprocessableException UnknownMetricKey(string key) =>
        new($"The metric key '{key}' is neither proposed in this run nor a metric of the machine.");

    public static UnprocessableException UnknownStatusCode(string code) =>
        new($"The status code '{code}' is neither proposed in this run nor a status of the machine.");

    public static UnprocessableException Rejected(string message) => new(message);
}
