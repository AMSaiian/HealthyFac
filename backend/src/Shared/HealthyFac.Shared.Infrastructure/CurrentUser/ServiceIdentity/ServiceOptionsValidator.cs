using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;

public sealed class ServiceOptionsValidator : AbstractValidator<ServiceOptions>
{
    public ServiceOptionsValidator()
    {
        RuleFor(options => options.Name)
            .Must(name => ServiceName.All.Any(service => service.Value == name))
            .WithMessage(ServiceValidationMessages.NameInvalid);
    }
}
