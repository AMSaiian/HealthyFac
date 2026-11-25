namespace HealthyFac.Shared.Web.Rpc;

/// <summary>
/// Defines the error texts of the gRPC services.
/// </summary>
public static class RpcErrors
{
    public const string IdInvalid = "The entity does not exist.";
    public const string DateInvalid = "The date must have the form yyyy-MM-dd.";
    public const string TimestampRequired = "The timestamp is required.";
    public const string EnumerationInvalid = "A value is not known.";
}
