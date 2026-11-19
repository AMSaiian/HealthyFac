namespace HealthyFac.Core.Domain.Shifts;

/// <summary>
/// One work block of a resolved day, in company-zone local times and as UTC instants.
/// </summary>
public sealed class DayBlock
{
    public required TimeOnly Start { get; init; }

    public required TimeOnly End { get; init; }

    public required DateTime StartUtc { get; init; }

    public required DateTime EndUtc { get; init; }
}
