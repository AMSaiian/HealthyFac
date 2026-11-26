using FluentValidation;

namespace HealthyFac.Telemetry.Application.Evaluation;

/// <summary>
/// Sets every value of the signal detection: the evaluation rhythm, the statistics, the rules, the phase matching,
/// and the lease of a machine run. No code assumes a value: the values come from <c>appsettings.json</c>. Time
/// constants are in seconds.
/// </summary>
public sealed record DetectionOptions
{
    public const string SectionName = "Telemetry:Detection";

    /// <summary>Turns the evaluator worker on. The tests that run the evaluation by hand turn it off.</summary>
    public bool IsWorkerEnabled { get; init; } = true;

    /// <summary>The time between two ticks of the evaluator worker.</summary>
    public int EvaluationIntervalSeconds { get; init; }

    /// <summary>A sample is evaluated only when it is older than this lag, so a message that arrives a little late
    /// still counts.</summary>
    public int SettleLagSeconds { get; init; }

    /// <summary>The number of samples of a metric and phase that form the first baseline.</summary>
    public int WarmUpSamples { get; init; }

    /// <summary>The time constant of the slow mean (the baseline).</summary>
    public int SlowTauSeconds { get; init; }

    /// <summary>The time constant of the fast mean.</summary>
    public int FastTauSeconds { get; init; }

    /// <summary>A drift opens when the absolute z value is above this limit.</summary>
    public double DriftLimit { get; init; }

    /// <summary>A drift resolves when the absolute z value is below this limit.</summary>
    public double DriftResolveLimit { get; init; }

    /// <summary>The time constant of the level of the trend filter.</summary>
    public int LevelTauSeconds { get; init; }

    /// <summary>The time constant of the slope of the trend filter.</summary>
    public int TrendTauSeconds { get; init; }

    /// <summary>A trend opens when the forecast reaches the alarm limit within this time.</summary>
    public int HorizonSeconds { get; init; }

    /// <summary>A level metric opens a trend only when the slope over the horizon exceeds this many sigmas.</summary>
    public double NoiseGuardFactor { get; init; }

    /// <summary>The number of samples in a row that a rule needs to open, escalate, or resolve.</summary>
    public int Persistence { get; init; }

    /// <summary>A gap between two samples above this time restarts the tracker of a metric.</summary>
    public int GapSeconds { get; init; }

    /// <summary>The share of the metric range that the sigma never falls below.</summary>
    public double SigmaFloorShare { get; init; }

    /// <summary>The cap of readings that one run of a machine reads.</summary>
    public int CatchUpBatchReadings { get; init; }

    /// <summary>An operational machine that sends nothing for this time opens a silent machine episode.</summary>
    public int SilenceLimitSeconds { get; init; }

    /// <summary>A mismatch of the phase shorter than this time keeps the current phase.</summary>
    public int PhaseDwellSeconds { get; init; }

    /// <summary>A machine that matches no phase for this time opens an unknown mode episode.</summary>
    public int UnknownModeDwellSeconds { get; init; }

    /// <summary>The time after a phase switch in which no statistics learn.</summary>
    public int SettleTimeSeconds { get; init; }

    /// <summary>The most buckets that one metric history request may ask for.</summary>
    public int HistoryBucketCap { get; init; }

    /// <summary>The time that a worker owns a machine run. A later worker takes over an expired lease.</summary>
    public int LeaseSeconds { get; init; }
}

/// <summary>
/// Validates the detection settings at startup.
/// </summary>
public sealed class DetectionOptionsValidator : AbstractValidator<DetectionOptions>
{
    public DetectionOptionsValidator()
    {
        RuleFor(options => options.EvaluationIntervalSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.SettleLagSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.WarmUpSamples)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.SlowTauSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.FastTauSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.DriftLimit)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.DriftResolveLimit)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.LevelTauSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.TrendTauSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.HorizonSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.NoiseGuardFactor)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.Persistence)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.GapSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.SigmaFloorShare)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.CatchUpBatchReadings)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.SilenceLimitSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.PhaseDwellSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.UnknownModeDwellSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.SettleTimeSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.LeaseSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.HistoryBucketCap)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(DetectionMessages.ValueNotPositive);

        RuleFor(options => options.FastTauSeconds)
            .LessThan(options => options.SlowTauSeconds)
            .WithMessage(DetectionMessages.FastTauNotBelowSlowTau);

        RuleFor(options => options.DriftResolveLimit)
            .LessThan(options => options.DriftLimit)
            .WithMessage(DetectionMessages.DriftResolveLimitNotBelowDriftLimit);

        RuleFor(options => options.PhaseDwellSeconds)
            .LessThan(options => options.UnknownModeDwellSeconds)
            .WithMessage(DetectionMessages.PhaseDwellNotBelowUnknownModeDwell);
    }
}
