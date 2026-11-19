using FluentValidation;

namespace HealthyFac.Insights.Application.Ingest.Screening;

/// <summary>
/// Holds the settings of the screening of the chunks of a file. The values come from <c>appsettings.json</c>.
/// </summary>
public sealed class ScreeningOptions
{
    public const string SectionName = "Insights:Screening";

    public const string BatchSizeInvalid = "Insights:Screening:BatchSize must be above 0.";

    /// <summary>The number of chunks that one model call screens.</summary>
    public int BatchSize { get; init; }
}

public sealed class ScreeningOptionsValidator : AbstractValidator<ScreeningOptions>
{
    public ScreeningOptionsValidator()
    {
        RuleFor(options => options.BatchSize)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(ScreeningOptions.BatchSizeInvalid);
    }
}
