namespace HealthyFac.Core.Infrastructure.Employees.InviteTokens;

public sealed class InviteTokenOptions
{
    public const string SectionName = "InviteTokens";

    public int LifetimeHours { get; init; } = InviteTokenConstants.Lifetime.DefaultHours;

    public List<InviteTokenKeyOptions> Keys { get; init; } = [];
}
