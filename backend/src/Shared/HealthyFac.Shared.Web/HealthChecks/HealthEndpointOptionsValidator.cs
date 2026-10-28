using System.Net;
using FluentValidation;

namespace HealthyFac.Shared.Web.HealthChecks;

public sealed class HealthEndpointOptionsValidator : AbstractValidator<HealthEndpointOptions>
{
    public HealthEndpointOptionsValidator()
    {
        RuleFor(options => options.Port)
            .GreaterThan(IPEndPoint.MinPort)
            .LessThanOrEqualTo(IPEndPoint.MaxPort);
    }
}
