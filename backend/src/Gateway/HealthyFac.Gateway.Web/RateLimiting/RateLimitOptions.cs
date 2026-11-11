using FluentValidation;

namespace HealthyFac.Gateway.Web.RateLimiting;

/// <summary>
/// Configures the limits per client address. The strict limit protects the anonymous invite endpoints.
/// The blob download limit protects the anonymous file download route. The static files limit covers the web app
/// files and the sign-in theme files, which a page reload requests without a token. The default limit applies to
/// every other route that the gateway proxies.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public FixedWindowLimit Strict { get; init; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    public FixedWindowLimit Default { get; init; } = new() { PermitLimit = 100, WindowSeconds = 60 };

    /// <summary>The limit of the anonymous file download route. A page with many images needs room.</summary>
    public FixedWindowLimit BlobDownload { get; init; } = new() { PermitLimit = 120, WindowSeconds = 60 };

    /// <summary>The limit of the web app and sign-in theme file routes. A reload requests many files at once.</summary>
    public FixedWindowLimit StaticAssets { get; init; } = new() { PermitLimit = 1000, WindowSeconds = 60 };
}

public sealed class RateLimitOptionsValidator : AbstractValidator<RateLimitOptions>
{
    public RateLimitOptionsValidator()
    {
        RuleFor(options => options.Strict.PermitLimit)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .WithMessage(RateLimitValidationMessages.Strict.PermitLimitTooSmall);

        RuleFor(options => options.Strict.WindowSeconds)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .WithMessage(RateLimitValidationMessages.Strict.WindowTooSmall);

        RuleFor(options => options.Default.PermitLimit)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .WithMessage(RateLimitValidationMessages.Default.PermitLimitTooSmall);

        RuleFor(options => options.Default.WindowSeconds)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .WithMessage(RateLimitValidationMessages.Default.WindowTooSmall);

        RuleFor(options => options.BlobDownload.PermitLimit)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .WithMessage(RateLimitValidationMessages.BlobDownload.PermitLimitTooSmall);

        RuleFor(options => options.BlobDownload.WindowSeconds)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .WithMessage(RateLimitValidationMessages.BlobDownload.WindowTooSmall);

        RuleFor(options => options.StaticAssets.PermitLimit)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .WithMessage(RateLimitValidationMessages.StaticAssets.PermitLimitTooSmall);

        RuleFor(options => options.StaticAssets.WindowSeconds)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .WithMessage(RateLimitValidationMessages.StaticAssets.WindowTooSmall);
    }
}
