using FluentValidation;

namespace HealthyFac.Insights.Application.Agents;

/// <summary>
/// Holds the settings of the agent runs: the debounce of a triage, the provider retry, the limits of the tools, and
/// the lease of a run. The values come from <c>appsettings.json</c>.
/// </summary>
public sealed class AgentOptions
{
    public const string SectionName = "Insights:Agent";

    /// <summary>Tells whether the host runs the agent worker. A test host of the API switches it off.</summary>
    public bool IsWorkerEnabled { get; init; } = true;

    /// <summary>The time in seconds that a triage waits for more signals of its case before it runs.</summary>
    public int RunDebounceSeconds { get; init; }

    /// <summary>The delays in seconds before each retry of a failed provider call. One entry for each retry.</summary>
    public IReadOnlyList<int> ProviderRetryDelaysSeconds { get; init; } = [];

    /// <summary>The most model turns of a planner run.</summary>
    public int PlannerMaxIterations { get; init; }

    /// <summary>The minutes of metric history that the planner reads before the signal.</summary>
    public int PlannerHistoryMinutes { get; init; }

    /// <summary>The number of buckets of a metric history that a tool asks for.</summary>
    public int PlannerHistoryBucketCount { get; init; }

    /// <summary>The longest range in hours that a history tool accepts.</summary>
    public int HistoryMaxRangeHours { get; init; }

    /// <summary>The number of manual chunks that the manual search tool returns.</summary>
    public int ManualSearchChunks { get; init; }

    /// <summary>The number of open work orders that the read tool returns.</summary>
    public int OpenWorkOrderCount { get; init; }

    /// <summary>The range in hours of the episode read tool.</summary>
    public int EpisodeRangeHours { get; init; }

    /// <summary>The most episodes that the episode read tool returns.</summary>
    public int EpisodeCount { get; init; }

    /// <summary>The most model turns of an extractor run.</summary>
    public int ExtractorMaxIterations { get; init; }

    /// <summary>The most tool calls in a row that fail before a run stops.</summary>
    public int MaxConsecutiveErrors { get; init; }

    /// <summary>The delay in seconds of the worker loop when no run is due.</summary>
    public int PollSeconds { get; init; }

    /// <summary>The time in seconds that a worker owns a run. After it, a sweep interrupts the run.</summary>
    public int RunLeaseSeconds { get; init; }
}

public sealed class AgentOptionsValidator : AbstractValidator<AgentOptions>
{
    public AgentOptionsValidator()
    {
        RuleFor(options => options.RunDebounceSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(AgentMessages.ValueNotPositive);

        RuleFor(options => options.PlannerMaxIterations)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(AgentMessages.ValueNotPositive);

        RuleFor(options => options.PlannerHistoryMinutes)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(AgentMessages.ValueNotPositive);

        RuleFor(options => options.PlannerHistoryBucketCount)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(AgentMessages.ValueNotPositive);

        RuleFor(options => options.HistoryMaxRangeHours)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(AgentMessages.ValueNotPositive);

        RuleFor(options => options.ManualSearchChunks)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(AgentMessages.ValueNotPositive);

        RuleFor(options => options.OpenWorkOrderCount)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(AgentMessages.ValueNotPositive);

        RuleFor(options => options.EpisodeRangeHours)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(AgentMessages.ValueNotPositive);

        RuleFor(options => options.EpisodeCount)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(AgentMessages.ValueNotPositive);

        RuleFor(options => options.ExtractorMaxIterations)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(AgentMessages.ValueNotPositive);

        RuleFor(options => options.MaxConsecutiveErrors)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(AgentMessages.ValueNotPositive);

        RuleFor(options => options.RunLeaseSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(AgentMessages.ValueNotPositive);

        RuleFor(options => options.PollSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(AgentMessages.ValueNotPositive);

        RuleForEach(options => options.ProviderRetryDelaysSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(AgentMessages.ValueNotPositive);
    }
}
