namespace HealthyFac.Telemetry.Domain.Intake;

/// <summary>
/// A status that was active at a sampling time. The reading has the natural primary key
/// (status definition, sampling time) and no <c>Id</c>.
/// </summary>
public class StatusReading
{
    public StatusReading(Guid statusDefinitionId, DateTime sampledAt)
    {
        StatusDefinitionId = statusDefinitionId;
        SampledAt = sampledAt;
    }

    public Guid StatusDefinitionId { get; private set; }
    public DateTime SampledAt { get; private set; }
}
