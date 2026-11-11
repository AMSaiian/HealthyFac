using FluentValidation;

namespace HealthyFac.Insights.Application.Ingest.Processing;

public sealed class IngestProcessingOptions
{
    public const string SectionName = "Ingest";

    /// <summary>Tells whether the host runs the ingest worker. A test host of the API switches it off.</summary>
    public bool IsWorkerEnabled { get; init; } = true;

    /// <summary>The longest run of one attempt in seconds. After it, the attempt fails (conversionTimeout).</summary>
    public int TimeoutSeconds { get; init; } = 900;

    /// <summary>The seconds that the lease outlasts the run limit, so a sweep never stops a live run.</summary>
    public int LeaseMarginSeconds { get; init; } = 120;

    /// <summary>The delay in seconds of the worker loop when no attempt is waiting.</summary>
    public int PollSeconds { get; init; } = 2;

    /// <summary>The number of worker loops. Only 1 is supported for now.</summary>
    public int WorkerCount { get; init; } = 1;

    /// <summary>The most pages of a file. A longer file fails with <c>tooManyPages</c>.</summary>
    public int MaxPages { get; init; } = 500;

    /// <summary>The first delay in milliseconds before a retry of an external call. Each retry doubles it.</summary>
    public int RetryDelayMilliseconds { get; init; } = 1000;
}

public sealed class IngestProcessingOptionsValidator : AbstractValidator<IngestProcessingOptions>
{
    public IngestProcessingOptionsValidator()
    {
        RuleFor(options => options.TimeoutSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(IngestProcessingMessages.TimeoutInvalid);
        RuleFor(options => options.LeaseMarginSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(IngestProcessingMessages.LeaseMarginInvalid);
        RuleFor(options => options.PollSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(IngestProcessingMessages.PollInvalid);
        RuleFor(options => options.WorkerCount)
            .InclusiveBetween(from: 1, to: 1)
            .WithMessage(IngestProcessingMessages.WorkerCountInvalid);
        RuleFor(options => options.MaxPages)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(IngestProcessingMessages.MaxPagesInvalid);
        RuleFor(options => options.RetryDelayMilliseconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(IngestProcessingMessages.RetryDelayInvalid);
    }
}
