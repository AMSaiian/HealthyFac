namespace HealthyFac.Spa.Web.RuntimeConfig;

/// <summary>
/// The content of <c>/config.json</c>. The file is public: add only values that every visitor may see.
/// </summary>
public sealed class RuntimeConfigResponse
{
    public required string ApiBaseUrl { get; init; }
}
