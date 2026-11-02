using HealthyFac.Shared.Web.Proxy;

namespace HealthyFac.Shared.UnitTests.Proxy.Data;

public static class ProxyOptionsValidatorTestData
{
    public const string ValidProxy = "10.0.0.7";
    public const string ValidNetwork = "10.0.0.0/8";
    public const string InvalidProxy = "10.0.0.256";
    public const string InvalidNetwork = "10.0.0.0/33";

    public static ProxyOptions ValidAddresses => new()
    {
        KnownProxies = [ValidProxy],
        KnownNetworks = [ValidNetwork],
    };

    public static TheoryData<ProxyOptions, string> InvalidAddressesWithExpectedError => new()
    {
        {
            new ProxyOptions { KnownProxies = [InvalidProxy], KnownNetworks = [ValidNetwork] },
            ProxyValidationMessages.ProxyAddressInvalid
        },
        {
            new ProxyOptions { KnownProxies = [ValidProxy], KnownNetworks = [InvalidNetwork] },
            ProxyValidationMessages.NetworkInvalid
        },
    };
}
