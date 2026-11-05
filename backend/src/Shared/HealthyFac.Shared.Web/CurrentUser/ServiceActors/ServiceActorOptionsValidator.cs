using FluentValidation;

namespace HealthyFac.Shared.Web.CurrentUser;

public sealed class ServiceActorOptionsValidator : AbstractValidator<ServiceActorOptions>
{
    public ServiceActorOptionsValidator()
    {
        RuleForEach(options => options.AllowedClients)
            .NotEmpty()
            .WithMessage(ServiceActorValidationMessages.ClientBlank);

        RuleFor(options => options.AllowedClients)
            .Must(clients => clients.Distinct(StringComparer.Ordinal).Count() == clients.Count)
            .WithMessage(ServiceActorValidationMessages.ClientDuplicated);
    }
}
