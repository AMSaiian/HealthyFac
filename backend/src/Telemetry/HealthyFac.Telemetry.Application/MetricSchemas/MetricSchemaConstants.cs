namespace HealthyFac.Telemetry.Application.MetricSchemas;

/// <summary>
/// Defines the names of the payload schema: the fields of the envelope, the annotation keywords, and the format of
/// the schema ID. The generator and the intake read the stored schema through these names.
/// </summary>
public static class MetricSchemaConstants
{
    public const string IdFormat = "urn:healthyfac:gateways:{0}:metrics:{1}";

    public static class Fields
    {
        public const string SampledAt = "sampledAt";
        public const string Metrics = "metrics";
        public const string Statuses = "statuses";
    }

    /// <summary>
    /// The annotation keywords. They describe a key and never assert, so a value outside the range or the states
    /// stays valid.
    /// </summary>
    public static class Annotations
    {
        public const string Range = "x-healthyfac-range";
        public const string States = "x-healthyfac-states";
        public const string Unit = "x-healthyfac-unit";
        public const string RangeMin = "min";
        public const string RangeMax = "max";
        public const string UnitQuantity = "quantity";
        public const string UnitName = "name";
    }
}
