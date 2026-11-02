using FluentValidation;

namespace HealthyFac.Shared.Infrastructure.Caching;

public sealed class RedisOptionsValidator : AbstractValidator<RedisOptions>
{
    public RedisOptionsValidator()
    {
        RuleFor(options => options.ConnectionString)
            .NotEmpty()
            .WithMessage(RedisValidationMessages.ConnectionStringRequired);

        RuleFor(options => options.ConnectTimeoutMilliseconds)
            .InclusiveBetween(RedisConstants.MinTimeoutMilliseconds, RedisConstants.MaxTimeoutMilliseconds)
            .WithMessage(RedisValidationMessages.ConnectTimeoutOutOfRange);

        RuleFor(options => options.OperationTimeoutMilliseconds)
            .InclusiveBetween(RedisConstants.MinTimeoutMilliseconds, RedisConstants.MaxTimeoutMilliseconds)
            .WithMessage(RedisValidationMessages.OperationTimeoutOutOfRange);
    }
}
