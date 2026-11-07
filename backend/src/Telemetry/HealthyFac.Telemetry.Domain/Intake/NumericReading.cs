namespace HealthyFac.Telemetry.Domain.Intake;

/// <summary>
/// A valid number of a metric at a sampling time. The reading has no <c>Id</c>: its natural key
/// (metric definition, sampling time) is the primary key, which also drops a repeated value, and nothing references
/// a single reading. A value outside the range is stored with <see cref="IsOutOfRange"/>.
/// </summary>
public class NumericReading
{
    public NumericReading(Guid metricDefinitionId, DateTime sampledAt, double value, bool isOutOfRange)
    {
        MetricDefinitionId = metricDefinitionId;
        SampledAt = sampledAt;
        Value = value;
        IsOutOfRange = isOutOfRange;
    }

    public Guid MetricDefinitionId { get; private set; }
    public DateTime SampledAt { get; private set; }
    public double Value { get; private set; }

    /// <summary>The value lay outside the range of the definition at receipt. A later range change does not
    /// reclassify it.</summary>
    public bool IsOutOfRange { get; private set; }
}
