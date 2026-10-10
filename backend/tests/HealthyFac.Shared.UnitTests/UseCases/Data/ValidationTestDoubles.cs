using FluentValidation;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Shared.UnitTests.UseCases.Data;

public sealed record ValidatedRequest : IRequest<int>
{
    public required string Name { get; init; }
}

public sealed class ValidatedRequestValidator : AbstractValidator<ValidatedRequest>
{
    public ValidatedRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().WithMessage(ValidationMessages.NameRequired);
    }
}

public sealed class FixedResultHandler : IRequestHandler<ValidatedRequest, int>
{
    public Task<int> HandleAsync(ValidatedRequest request, CancellationToken ct = default)
        => Task.FromResult(request.Name.Length);
}

public static class ValidationMessages
{
    public const string NameRequired = "Name is required.";
}
