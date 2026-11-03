namespace HealthyFac.Spa.Web.RuntimeConfig;

/// <summary>
/// The content of <c>/config.json</c>. The file is public: add only values that every visitor may see.
/// </summary>
public sealed class RuntimeConfigResponse
{
    public required string ApiBaseUrl { get; init; }

    public required string AuthUrl { get; init; }

    public required string AuthRealm { get; init; }

    public required string AuthClientId { get; init; }
}
