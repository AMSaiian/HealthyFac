using System.Net;
using FluentValidation;

namespace HealthyFac.Shared.Web.Proxy;

/// <summary>
/// Configures which reverse proxies the service trusts for forwarded headers.
/// The service reads the client address and scheme from those headers, so an untrusted proxy list lets a
/// caller spoof its own address.
/// </summary>
public sealed class ProxyOptions
{
    public const string SectionName = "Proxy";

    /// <summary>Set to false to ignore forwarded headers completely.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Addresses of the proxies that may set forwarded headers, for example "10.0.0.7".</summary>
    public string[] KnownProxies { get; init; } = [];

    /// <summary>Networks that may set forwarded headers, in CIDR form, for example "10.0.0.0/8".</summary>
    public string[] KnownNetworks { get; init; } = [];

    /// <summary>How many proxy hops to walk back through. Keep this at the number of proxies you run.</summary>
    public int ForwardLimit { get; init; } = 1;

    /// <summary>
    /// Trusts every hop and clears the proxy and network lists. Use this only when the service is
    /// unreachable except through the proxy. A directly reachable service lets any caller spoof its address.
    /// </summary>
    public bool IsEveryProxyTrusted { get; init; }
}

public sealed class ProxyOptionsValidator : AbstractValidator<ProxyOptions>
{
    public ProxyOptionsValidator()
    {
        RuleFor(options => options.ForwardLimit)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .When(options => !options.IsEveryProxyTrusted)
            .WithMessage(ProxyValidationMessages.ForwardLimitTooSmall);

        RuleForEach(options => options.KnownProxies)
            .Must(proxy => IPAddress.TryParse(proxy, out _))
            .WithMessage(ProxyValidationMessages.ProxyAddressInvalid);

        RuleForEach(options => options.KnownNetworks)
            .Must(network => IPNetwork.TryParse(network, out _))
            .WithMessage(ProxyValidationMessages.NetworkInvalid);
    }
}
