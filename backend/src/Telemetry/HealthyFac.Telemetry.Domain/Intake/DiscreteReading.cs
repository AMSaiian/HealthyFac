namespace HealthyFac.Telemetry.Domain.Intake;

/// <summary>
/// A valid text value of a metric at a sampling time: a state code, or <c>true</c> or <c>false</c> for a boolean. The
/// reading has the natural primary key of <see cref="NumericReading"/> and no <c>Id</c>.
/// </summary>
public class DiscreteReading
{
    public DiscreteReading(Guid metricDefinitionId, DateTime sampledAt, string value, bool isOutOfRange)
    {
        MetricDefinitionId = metricDefinitionId;
        SampledAt = sampledAt;
        Value = value;
        IsOutOfRange = isOutOfRange;
    }

    public Guid MetricDefinitionId { get; private set; }
    public DateTime SampledAt { get; private set; }
    public string Value { get; private set; }

    /// <summary>The state lay outside the states of the definition at receipt.</summary>
    public bool IsOutOfRange { get; private set; }
}
