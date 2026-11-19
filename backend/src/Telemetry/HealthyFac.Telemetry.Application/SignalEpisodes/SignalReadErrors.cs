namespace HealthyFac.Telemetry.Application.SignalEpisodes;

/// <summary>
/// Defines the error messages of the signal read use cases.
/// </summary>
public static class SignalReadErrors
{
    public const string CompanyIdRequired = "CompanyId is required.";
    public const string MachineIdRequired = "MachineId is required.";
    public const string MetricIdRequired = "MetricDefinitionId is required.";
    public const string RangeInvalid = "The end of the range must be after its start.";
    public const string MetricNotFound = "The metric does not exist.";

    public static string BucketCountOutOfRange(int cap) =>
        $"BucketCount must be between 1 and {cap}.";
}
