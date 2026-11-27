using FluentValidation;

namespace HealthyFac.Insights.Infrastructure.Caching;

/// <summary>
/// The Redis caches of the Insights service. Each cache has its own limits.
/// </summary>
public sealed class InsightsCacheOptions
{
    public const string SectionName = "Caches";

    /// <summary>The copy of the unit catalog of Telemetry.</summary>
    public RedisFetchCacheOptions MetricUnits { get; init; } = new();

    /// <summary>The copy of the time zone of each company.</summary>
    public RedisFetchCacheOptions CompanyZones { get; init; } = new();
}

public sealed class InsightsCacheOptionsValidator : AbstractValidator<InsightsCacheOptions>
{
    public InsightsCacheOptionsValidator()
    {
        RuleFor(options => options.MetricUnits).SetValidator(new RedisFetchCacheOptionsValidator());
        RuleFor(options => options.CompanyZones).SetValidator(new RedisFetchCacheOptionsValidator());
    }
}
