namespace HealthyFac.Telemetry.Application.MetricSchemas;

/// <summary>
/// Builds the JSON Schema (draft 2020-12) of the payload of one machine. The schema is the only definition of the
/// payload rules. Infrastructure implements it, so only that layer references the schema library.
/// </summary>
public interface IMetricSchemaGenerator
{
    /// <summary>
    /// Builds the schema of a machine for one revision.
    /// </summary>
    /// <param name="gatewayId">The gateway ID of the machine. It is part of the schema ID.</param>
    /// <param name="revision">The revision number. It is part of the schema ID.</param>
    /// <param name="metrics">The active metric definitions. An empty list gives an empty <c>metrics</c> object.</param>
    /// <returns>The schema as JSON text.</returns>
    string Generate(string gatewayId, int revision, IEnumerable<MetricSchema> metrics);
}
