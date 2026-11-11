namespace HealthyFac.Core.IntegrationTests.Database.Data;

/// <summary>
/// Follows the system clock until a test freezes it. The fixture releases it before each test, so a test that does not
/// freeze it sees the real time, like before.
/// </summary>
public sealed class ControllableTimeProvider : TimeProvider
{
    private DateTimeOffset? _frozenAt;

    public void Freeze(DateTimeOffset at) => _frozenAt = at;

    public void Release() => _frozenAt = null;

    public override DateTimeOffset GetUtcNow() => _frozenAt ?? System.GetUtcNow();
}
