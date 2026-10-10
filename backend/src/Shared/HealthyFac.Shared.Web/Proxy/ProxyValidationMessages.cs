namespace HealthyFac.Shared.Web.Proxy;

/// <summary>
/// Defines the validator messages for the reverse proxy configuration.
/// </summary>
public static class ProxyValidationMessages
{
    public const string ForwardLimitTooSmall = "The forward limit must be at least 1.";
    public const string ProxyAddressInvalid = "Each known proxy must be a valid IP address, such as '10.0.0.7'.";
    public const string NetworkInvalid = "Each known network must use CIDR form, such as '10.0.0.0/8'.";
}
