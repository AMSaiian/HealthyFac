using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.TelemetryHistory;

/// <summary>
/// Defines the error messages of the telemetry history part. Each message names the item and the rule.
/// </summary>
public static class TelemetryHistoryErrors
{
    public static string WindowInvalid(string item) => $"The item {item} must be above 0.";

    public static string MachineUnknown(string item, string machine) =>
        $"The item {item} names the machine {machine}, which machines.json does not hold.";

    public static string ModuleUnknown(string item, string module, string machine) =>
        $"The item {item} names the module {module}, which the scenario of the machine {machine} does not have.";

    public static string MetricUnknown(string item, string metric, string machine) =>
        $"The item {item} names the metric {metric}, which the draft of the machine {machine} does not have.";

    public static string PersonUnknown(string item, string person) =>
        $"The item {item} names the person {person}, which company.json does not hold.";

    public static string MomentNotInHours(string item, RelativeMoment moment) =>
        $"The item {item} has the moment {moment}. "
        + "A moment of telemetry.json must use the hour form (h±N or h±N:MM).";

    public static string MomentOutsideWindow(string item, RelativeMoment moment, int windowHours) =>
        $"The item {item} has the moment {moment}, "
        + $"which lies outside the window of {windowHours} hours before the job.";

    public static string EndNotAfterStart(string item) => $"The item {item} ends at or before its start.";

    public static string DrainHoursInvalid(string item) => $"The item {item} needs DrainHours above 0.";

    public static string MaintenanceOutsideDrainRange(string item) =>
        $"The item {item} starts the maintenance before "
        + $"{Math.Round(ModuleState.AlarmProgress * 100)} % of the drain, "
        + "or at or after the end of the drain.";

    public static string KeyDuplicated(string item, string firstList, string secondList) =>
        $"The item {item} has a key that the lists {firstList} and {secondList} both use. Each key must be unique.";

    public static string StatusCodeUnknown(string item, string code, string machine) =>
        $"The item {item} names the status code {code}, which the draft of the machine {machine} does not have.";

    public static string PayloadRejected(string gatewayId, DateTime sampledAt, string code) =>
        $"The schema of the machine {gatewayId} rejects the history message of {sampledAt:O} with the error {code}.";

    public static string ColumnTypeNotSupported(string typeName) =>
        $"The bulk writer does not support the column value type {typeName}.";

    public static string AnchorSampleMissing(string machine) =>
        $"The machine {machine} has no sample at the anchor, so the marker message cannot be written.";

    public static string StepMetricNotNumeric(string item, string metric) =>
        $"The item {item} steps the metric {metric}, which is not numeric.";

    public static string OverrideValueInvalid(string item, string metric, string kind) =>
        $"The item {item} gives the metric {metric} a value that does not fit its value kind {kind}.";

    public static string WorkOrderNotBeforeLink(string item) =>
        $"The item {item} must be assigned before the link moment, which lies "
        + $"{TelemetryHistoryConstants.LinkLeadMinutes} minute before the maintenance. "
        + $"The assignment follows the creation by {TelemetryHistoryConstants.AssignDelayMinutes} minutes.";

    public static string LinkNeedsOpenCase(string item, string machine) =>
        $"The event {item} links a work order, but the machine {machine} has no open case at its moment.";

    public static string NoWorkBlock(string item, string assignee) =>
        $"The event {item} finds no work block of {assignee} that holds the estimate in the next days.";
}
