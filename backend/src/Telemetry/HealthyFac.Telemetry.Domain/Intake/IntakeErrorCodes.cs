namespace HealthyFac.Telemetry.Domain.Intake;

/// <summary>
/// Defines the codes of the errors that a received message can carry. The camelCase values are stored in the
/// database, so never rename one in place.
/// </summary>
public static class IntakeErrorCodes
{
    public const string MalformedJson = "malformedJson";
    public const string UnstorableJson = "unstorableJson";
    public const string ProcessingFailed = "processingFailed";
    public const string MalformedGatewayId = "malformedGatewayId";
    public const string UnknownGateway = "unknownGateway";
    public const string MissingField = "missingField";
    public const string UnknownField = "unknownField";
    public const string InvalidSampledAt = "invalidSampledAt";
    public const string FutureSampledAt = "futureSampledAt";
    public const string WrongType = "wrongType";
    public const string UnknownMetric = "unknownMetric";
    public const string MissingMetric = "missingMetric";
    public const string UnknownStatus = "unknownStatus";
    public const string DuplicateStatus = "duplicateStatus";
}
