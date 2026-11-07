using FluentValidation;

namespace HealthyFac.Shared.Infrastructure.Caching;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public required string ConnectionString { get; init; }

    public int ConnectTimeoutMilliseconds { get; init; } = 1000;

    public int OperationTimeoutMilliseconds { get; init; } = 500;
}

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
