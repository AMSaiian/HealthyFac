namespace HealthyFac.Shared.Application.Interfaces;

/// <summary>
/// Supplies the current time in UTC. Application code reads time through this interface and never calls
/// <see cref="DateTime.UtcNow"/> directly, so a test can control the clock.
/// </summary>
public interface IDateProvider
{
    DateTime GetUtc();
}
