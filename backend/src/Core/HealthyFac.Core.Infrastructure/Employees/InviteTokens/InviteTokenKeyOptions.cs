namespace HealthyFac.Core.Infrastructure.Employees.InviteTokens;

public sealed class InviteTokenKeyOptions
{
    public required string Id { get; init; }

    public required string Secret { get; init; }

    public bool IsDefault { get; init; }
}
