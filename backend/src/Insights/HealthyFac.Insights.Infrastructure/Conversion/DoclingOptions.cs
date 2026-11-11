using FluentValidation;
using HealthyFac.Shared.Application.Configuration;

namespace HealthyFac.Insights.Infrastructure.Conversion;

public sealed class DoclingOptions
{
    public const string SectionName = "Docling";

    /// <summary>The address of docling-serve.</summary>
    public required string BaseUrl { get; init; }

    /// <summary>The value of the <c>X-Api-Key</c> header.</summary>
    public required string ApiKey { get; init; }

    /// <summary>The time in seconds between two polls of a conversion task.</summary>
    public int PollSeconds { get; init; } = 2;

    /// <summary>The most pages of a file. A longer file fails with the error code <c>tooManyPages</c>.</summary>
    public int MaxPages { get; init; } = 500;
}

public sealed class DoclingOptionsValidator : AbstractValidator<DoclingOptions>
{
    public DoclingOptionsValidator()
    {
        RuleFor(options => options.BaseUrl)
            .MustBeAbsoluteHttpUri()
            .WithMessage(DoclingMessages.BaseUrlInvalid);
        RuleFor(options => options.ApiKey)
            .NotEmpty()
            .WithMessage(DoclingMessages.ApiKeyRequired);
        RuleFor(options => options.PollSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DoclingMessages.PollSecondsInvalid);
        RuleFor(options => options.MaxPages)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DoclingMessages.MaxPagesInvalid);
    }
}
