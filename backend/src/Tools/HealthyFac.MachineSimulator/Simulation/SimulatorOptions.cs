using FluentValidation;

namespace HealthyFac.MachineSimulator.Simulation;

/// <summary>
/// Configures the timing and the randomness of the machine simulator.
/// </summary>
public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    /// <summary>The time between two messages of one machine.</summary>
    public TimeSpan PublishInterval { get; init; } = TimeSpan.FromSeconds(seconds: 1);

    /// <summary>The time between two reads of the Core state and the definitions of the known machines.</summary>
    public TimeSpan StatePollInterval { get; init; } = TimeSpan.FromSeconds(seconds: 5);

    /// <summary>The time between two reads that find new and removed machines.</summary>
    public TimeSpan DiscoveryInterval { get; init; } = TimeSpan.FromMinutes(minutes: 1);

    /// <summary>The run time that a draining module needs to reach its failure.</summary>
    public TimeSpan DrainDuration { get; init; } = TimeSpan.FromMinutes(minutes: 3);

    /// <summary>The seed of the noise and the generic values. <see langword="null"/> means a random seed.</summary>
    public int? RandomSeed { get; init; }
}

public sealed class SimulatorOptionsValidator : AbstractValidator<SimulatorOptions>
{
    public const string IntervalMessage = "Each simulator interval must be above 0 seconds.";

    public SimulatorOptionsValidator()
    {
        RuleFor(options => options.PublishInterval).GreaterThan(TimeSpan.Zero).WithMessage(IntervalMessage);
        RuleFor(options => options.StatePollInterval).GreaterThan(TimeSpan.Zero).WithMessage(IntervalMessage);
        RuleFor(options => options.DiscoveryInterval).GreaterThan(TimeSpan.Zero).WithMessage(IntervalMessage);
        RuleFor(options => options.DrainDuration).GreaterThan(TimeSpan.Zero).WithMessage(IntervalMessage);
    }
}
