using System.Net;
using FluentValidation;

namespace HealthyFac.Shared.Web.Proxy;

public sealed class ProxyOptionsValidator : AbstractValidator<ProxyOptions>
{
    public ProxyOptionsValidator()
    {
        RuleFor(options => options.ForwardLimit)
            .GreaterThanOrEqualTo(1)
            .When(options => !options.TrustAllProxies)
            .WithMessage(ProxyValidationMessages.ForwardLimitTooSmall);

        RuleForEach(options => options.KnownProxies)
            .Must(proxy => IPAddress.TryParse(proxy, out _))
            .WithMessage(ProxyValidationMessages.ProxyAddressInvalid);

        RuleForEach(options => options.KnownNetworks)
            .Must(network => IPNetwork.TryParse(network, out _))
            .WithMessage(ProxyValidationMessages.NetworkInvalid);
    }
}
